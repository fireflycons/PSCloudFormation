# PSake makes variables declared here available in other scriptblocks
# Init some things
Properties {
    # Find the build folder based on build system
    $ProjectRoot = $ENV:BHProjectPath
    if (-not $ProjectRoot)
    {
        $ProjectRoot = $PSScriptRoot
    }
    $ProjectRoot = Convert-Path $ProjectRoot

    try
    {
        $script:IsWindows = (-not (Get-Variable -Name IsWindows -ErrorAction Ignore)) -or $IsWindows
        $script:IsLinux = (Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux
        $script:IsMacOS = (Get-Variable -Name IsMacOS -ErrorAction Ignore) -and $IsMacOS
        $script:IsCoreCLR = $PSVersionTable.ContainsKey('PSEdition') -and $PSVersionTable.PSEdition -eq 'Core'
    }
    catch { }

    $Timestamp = Get-date -uformat "%Y%m%d-%H%M%S"
    $PSVersion = $PSVersionTable.PSVersion.Major
    $TestFile = "TestResults_PS$PSVersion`_$TimeStamp.xml"
    $lines = '----------------------------------------------------------------------'

    $Verbose = @{}
    if ($ENV:BHCommitMessage -match "!verbose")
    {
        $Verbose = @{Verbose = $True}
    }

    $DefaultLocale = 'en-US'
    $DocsRootDir = Join-Path $PSScriptRoot docs
    $ModuleName = "PSCloudFormation"
    $ModuleOutDir = Join-Path $PSScriptRoot PSCloudFormation

}

Task Default -Depends BuildHelp, Deploy

Task BuildAppVeyor -Depends Build

Task Init {
    $lines
    [Reflection.Assembly]::LoadWithPartialName("System.Security") | Out-Null
    Set-Location $ProjectRoot
    "Build System Details:"
    Get-ChildItem ENV: | Where-Object { $_.Name -like 'BH*' -or $_.Name -like 'AWS_TOOLS*' }
    "`n"

    if ($script:IsWindows)
    {
        "Checking for NuGet"
        $psgDir = Join-Path ${env:LOCALAPPDATA} "Microsoft\Windows\PowerShell\PowerShellGet"

        $nugetPath = $(

            $nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue

            if ($nuget)
            {
                $nuget.Path
            }
            else
            {
                if (Test-Path -Path (Join-Path $psgDir 'nuget.exe'))
                {
                    Join-Path $psgDir 'nuget.exe'
                }
            }
        )

        if ($nugetPath)
        {
            "NuGet.exe found at '$nugetPath"
        }
        else
        {
            if (-not (Test-Path -Path $psgDir -PathType Container))
            {
                New-Item -Path $psgDir -ItemType Directory | Out-Null
            }

            "Installing NuGet to '$psgDir'"
            Invoke-WebRequest -Uri https://nuget.org/nuget.exe -OutFile (Join-Path $psgDir 'nuget.exe')
        }
    }
}

Task ListModules -Depends Init {

    Write-Host "Available"
    Get-Module -ListAvailable | Select-Object ModuleType, Version, Name | Out-Host
    Write-Host "Loaded"
    Get-Module | Select-Object ModuleType, Version, Name | Out-Host
}

Task Build -Depends Init {
    $lines

    # Run Cake build on binary solution
    try
    {
        $Script = Join-Path $PSScriptRoot 'build.cake'
        $Target = 'Default'
        $Configuration = 'Release'
        $Verbosity = "Normal"
        $ForceCoreClr = $false
        $ScriptArgs = @()
        $SkipToolPackageRestore = $false
        $WhatIf = $false

        Write-Host "Preparing to run build script..."

        Write-Host "Checking operating system..."

        if ($script:IsWindows)
        {
            Write-Host " - Windows"
        }
        elseif ($script:IsLinux)
        {
            Write-Host " - Linux"
            $ForceCoreClr = $true
        }
        elseif ($script:IsMacOS)
        {
            Write-Host "- MacOS"
            $ForceCoreClr = $true
        }
        else
        {
            Write-Host " - Unknown: Cannot continue!"
            exit 1
        }

        if (!$PSScriptRoot)
        {
            $PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
        }

        $TOOLS_DIR = Join-Path $PSScriptRoot "tools"
        $PACKAGES_CONFIG = Join-Path $TOOLS_DIR "packages.config"
        $PACKAGES_CONFIG_MD5 = Join-Path $TOOLS_DIR "packages.config.md5sum"

        # Should we use the new Roslyn?
        $UseExperimental = [string]::Empty;
        if ($Experimental.IsPresent)
        {
            Write-Verbose -Message "Using experimental version of Roslyn."
            $UseExperimental = "-experimental"
        }

        # Is this a dry run?
        $UseDryRun = [string]::Empty;
        if ($WhatIf.IsPresent)
        {
            $UseDryRun = "-dryrun"
        }

        # Will we use dotnet to invoke Cake?
        $DotNet = [string]::Empty
        $DotNetExpression = [string]::Empty
        if ($ForceCoreClr)
        {
            try
            {
                $DotNet = (Get-Command dotnet -ErrorAction Stop).Source
                $DotNetExpression = "`"$DotNet`""
            }
            catch
            {
                throw "Unable to locate dotnet executable on this system!"
            }
        }

        # Make sure tools folder exists
        if ((Test-Path $PSScriptRoot) -and !(Test-Path $TOOLS_DIR))
        {
            Write-Verbose -Message "Creating tools directory..."
            New-Item -Path $TOOLS_DIR -Type directory | out-null
        }

        # Make sure that packages.config exist.
        if (!(Test-Path $PACKAGES_CONFIG))
        {
            Write-Verbose -Message "Downloading packages.config..."
            try
            {
                (New-Object System.Net.WebClient).DownloadFile("http://cakebuild.net/download/bootstrapper/packages", $PACKAGES_CONFIG)
            }
            catch
            {
                Throw "Could not download packages.config."
            }
        }

        if ($script:IsWindows -and -not $ForceCoreClr)
        {
            # Windows - Acquire Nuget and use to download Cake (.NET Framework)

            $NUGET_EXE = Join-Path $TOOLS_DIR "nuget.exe"
            $CAKE = Join-Path $TOOLS_DIR "Cake/Cake.exe"
            $NUGET_URL = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

            # Try find NuGet.exe in path if not exists
            if (!(Test-Path $NUGET_EXE))
            {
                Write-Verbose -Message "Trying to find nuget.exe in PATH..."
                $existingPaths = $Env:Path -Split ';' |
                Where-Object {

                    if ([string]::IsNullOrEmpty($_))
                    {
                        $false
                    }

                    try
                    {
                        # Some paths may throw Access Denied exceptions which write a message to STDERR, causing Bamboo to fail the build
                        Test-Path $_ -PathType Container
                    }
                    catch
                    {
                        $false
                    }
                }

                $NUGET_EXE_IN_PATH = Get-ChildItem -Path $existingPaths -Filter "nuget.exe" | Select-Object -First 1
                if ($null -ne $NUGET_EXE_IN_PATH -and (Test-Path $NUGET_EXE_IN_PATH.FullName))
                {
                    Write-Verbose -Message "Found in PATH at $($NUGET_EXE_IN_PATH.FullName)."
                    $NUGET_EXE = $NUGET_EXE_IN_PATH.FullName
                }
            }

            # Try download NuGet.exe if not exists
            if (!(Test-Path $NUGET_EXE))
            {
                Write-Verbose -Message "Downloading NuGet.exe..."
                try
                {
                    (New-Object System.Net.WebClient).DownloadFile($NUGET_URL, $NUGET_EXE)
                }
                catch
                {
                    Throw "Could not download NuGet.exe."
                }
            }

            # Save nuget.exe path to environment to be available to child processed
            $ENV:NUGET_EXE = $NUGET_EXE

            # Restore tools from NuGet?
            if (-Not $SkipToolPackageRestore.IsPresent)
            {
                Push-Location
                Set-Location $TOOLS_DIR

                # Check for changes in packages.config and remove installed tools if true.
                [string] $md5Hash = MD5HashFile($PACKAGES_CONFIG)
                if ((!(Test-Path $PACKAGES_CONFIG_MD5)) -Or
                    ($md5Hash -ne (Get-Content $PACKAGES_CONFIG_MD5 )))
                {
                    Write-Verbose -Message "Missing or changed package.config hash..."
                    Remove-Item * -Recurse -Exclude packages.config, nuget.exe
                }

                Write-Verbose -Message "Restoring tools from NuGet..."
                $NuGetOutput = Invoke-Expression "&`"$NUGET_EXE`" install -ExcludeVersion -OutputDirectory `"$TOOLS_DIR`""

                if ($LASTEXITCODE -ne 0)
                {
                    Throw "An error occured while restoring NuGet tools."
                }
                else
                {
                    $md5Hash | Out-File $PACKAGES_CONFIG_MD5 -Encoding "ASCII"
                }
                Write-Verbose -Message ($NuGetOutput | out-string)
                Pop-Location
            }

            # Make sure that Cake has been installed.
            if (!(Test-Path $CAKE))
            {
                Throw "Could not find Cake.exe at $CAKE"
            }
        }
        else
        {
            # Linux/CoreCLR
            # Use dotnet restore to get Cake CoreCLR since we do not have NuGet.exe
            # Now transform packages.config to a csproj file so that dotnet restore accepts it.

            $xsl = @'
<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:template match="/">
        <Project Sdk="Microsoft.NET.Sdk">
            <PropertyGroup>
                <TargetFramework>netstandard2.0</TargetFramework>
            </PropertyGroup>
            <ItemGroup>
                <xsl:for-each select="packages/package">
                    <PackageReference>
                        <xsl:attribute name="Include">
                            <xsl:choose>
                                <xsl:when test="@id = 'Cake'">Cake.CoreCLR</xsl:when>
                                <xsl:otherwise><xsl:value-of select="@id" /></xsl:otherwise>
                            </xsl:choose>
                        </xsl:attribute>
                        <xsl:attribute name="Version">
                            <xsl:value-of select="@version" />
                        </xsl:attribute>
                    </PackageReference>
                </xsl:for-each>
            </ItemGroup>
        </Project>
    </xsl:template>
</xsl:stylesheet>
'@
            $cakeCsproj = Join-Path $PSScriptRoot "cake-package-$([Guid]::NewGuid()).csproj"
            try
            {
                # XSLT transform packages.config to a .csproj file
                $bytes = [System.Text.Encoding]::UTF8.GetBytes($xsl)
                $stream = New-Object System.IO.MemoryStream (, $bytes)
                $reader = [System.Xml.XmlReader]::Create($stream)

                $xsltSettings = New-Object System.Xml.Xsl.XsltSettings;
                $xmlUrlResolver = New-Object System.Xml.xmlUrlResolver;
                $xsltSettings.EnableScript = 1;

                $xslt = New-Object System.Xml.Xsl.XslCompiledTransform;
                $xslt.Load($reader, $xsltSettings, $xmlUrlResolver);
                $xslt.Transform($PACKAGES_CONFIG, $cakeCsproj);

                # Run dotnet restore
                & $DotNet restore --packages $TOOLS_DIR $cakeCsproj

                # Now locate cake.dll
                $CAKE = Get-ChildItem -Path $TOOLS_DIR -File -Filter Cake.dll -Recurse |
                Select-Object -First 1 |
                Select-Object -ExpandProperty FullName

                if (-not $CAKE)
                {
                    throw "Could not find Cake.dll (Cake CoreCLR application)"
                }
            }
            catch
            {
                $errorMessage = $_.Exception.Message
                $failedItem = $_.Exception.ItemName
                Write-Host  'Error'$errorMessage':'$failedItem':' $_.Exception;
                exit 1
            }
            finally
            {
                $reader, $stream |
                Where-Object {
                    $null -ne $_
                } |
                Foreach-Object {
                    $_.Dispose()
                }

                if (Test-Path -Path $cakeCsproj -PathType Leaf)
                {
                    Remove-Item $cakeCsproj
                }
            }
        }

        Invoke-Command -NoNewScope {
            # Check for a prebuild script and include if present.
            Get-ChildItem -Path $PSScriptRoot -Filter *.cake |
            Where-Object {
                $_.Name -ieq 'prebuild.cake'
            } |
            Select-Object -First 1 |
            Select-Object -ExpandProperty FullName

            $Script
        } |
        Foreach-Object {

            $scriptToExecute = $_

            if (-not ($scriptToExecute.Contains('/') -or $scriptToExecute.Contains('\') -or (Test-Path -Path $scriptToExecute -PathType Leaf)))
            {
                # Assume build script is in same folder as this file
                $scriptToExecute = Join-Path $PSScriptRoot $scriptToExecute
            }

            Write-Host "`nExecuting $(Split-Path -Leaf $scriptToExecute) ..."

            # First load any Cake modules speficied by #module
            Invoke-Expression "& $DotNetExpression `"$CAKE`"  `"$scriptToExecute`" --bootstrap"

            # Now execute script
            Invoke-Expression "& $DotNetExpression `"$CAKE`" `"$scriptToExecute`" -target=`"$Target`" -configuration=`"$Configuration`" -verbosity=`"$Verbosity`" $ScriptArgs"

            if ($LASTEXITCODE -ne 0)
            {
                throw "$(Split-Path -Leaf $scriptToExecute) - Execution failed."
            }
        }
    }
    catch
    {
        Write-Host "Exception Thrown: $($_.Exception.Message)"
        Write-Host $_.ScriptStackTrace
        throw
    }
    finally
    {
        Write-Host
        Write-Host "Cake downloaded $([Math]::Round((Get-ChildItem (Join-Path $PSSCriptRoot tools) -File -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)) MB of tools and addins"
        Write-Host
    }

    # Update path to module manifest
    New-Item -Path Env:\ -Name BHPSModuleManifest -Value (Get-ChildItem (Get-Content -Raw (Join-Path $env:BHProjectPath ModulePath.txt)).Trim() -Filter *.psd1 | Select-Object -ExpandProperty FullName) -Force | Out-Null

}

Task CleanModule -Depends Build {

    $lines

    try
    {
        Push-Location (Split-Path -Parent $env:BHPSModuleManifest)

        @(
            'publish',
            '*.pdb'
            '*.json'
            'debug.ps1'
            'Firefly.PSCloudFormation.xml'
        ) |
        Where-Object {
            Test-Path -Path $_
        } |
        Foreach-Object {

            $recurse = @{}

            if (Test-Path -Path $_ -PathType Container)
            {
                $recurse.Add('Recurse', $true)
            }

            Remove-Item $_ @recurse
        }
    }
    finally
    {
        Pop-Location
    }
}

Task UpdateManifest -Depends CleanModule {

    # Load the module, read the exported commands, update the psd1 CmdletsToExport
    try
    {
        $params = @{
            Force = $True
            Passthru = $True
            Name = $env:BHPSModuleManifest
        }

        # Create a runspace, add script to run
        $PowerShell = [Powershell]::Create()
        [void]$PowerShell.AddScript({
            Param ($Force, $Passthru, $Name)
            $module = Import-Module -Name $Name -PassThru:$Passthru -Force:$Force
            $module | Where-Object {$_.Path -notin $module.Scripts}
        }).AddParameters($Params)

        #Consider moving this to a runspace or job to keep session clean
        $Module = $PowerShell.Invoke()

        if (-not $Module)
        {
            Throw "Could not find module '$Name'"
        }

        $cmdlets = $Module.ExportedCommands.Keys
    }
    finally
    {
        # Close down the runspace
        $PowerShell.Dispose()
    }

    Update-MetaData -Path $env:BHPSModuleManifest -PropertyName CmdletsToExport -Value @( $cmdlets )

    # Bump the module version if we didn't already
    Update-Metadata -Path $env:BHPSModuleManifest -PropertyName ModuleVersion -Value (Get-Content -Raw (Join-Path $PSScriptRoot "module.ver")).Trim() -ErrorAction stop

    # Set file list
    Update-Metadata -Path $env:BHPSModuleManifest -PropertyName FileList -Value (Get-ChildItem -Path (Split-Path -Parent $env:BHPSModuleManifest) -Exclude *.psd1).Name
}

Task Deploy -Depends UpdateManifest {

    $lines

    $deployParams = $(

        if ($ENV:BHBuildSystem -ieq 'AppVeyor')
        {
            # We will deploy _something_
            Write-Host '- Deploying to'
            Write-Host '  - AppVeyor Artifact'

            $params = @{
                Path  = $ProjectRoot
                Force = $true
                Tags = @('Development') # Push AppVeyor artifact
            }

            if ($ENV:BHBranchName -eq "master" -and $ENV:APPVEYOR_REPO_TAG -ieq 'true')
            {
                # Tag push in master is a release, so we want to also push to PSGallery
                $params['Tags'] += 'Production'
                Write-Host '  - PowerShell Gallery'
            }

            # Emit parameters
            $params
        }
        else
        {
            $null
        }
    )

    # Gate deployment
    if ($null -ne $deployParams)
    {
        Invoke-PSDeploy @Verbose @deployParams
    }
    else
    {
        "Skipping deployment: To deploy, ensure that...`n" +
        "`t* You are in AppVeyor (Current: $ENV:BHBuildSystem)`n" +
        "`t* For Gallery deployment`n" +
        "`t  * You are committing to the master branch (Current: $ENV:BHBranchName) `n" +
        "`t  * You have pushed a tag (i.e. created a release in GitHub)"
    }
}

Task BuildHelp -Depends Build, UpdateManifest, GenerateMarkdown {}

Task GenerateMarkdown -requiredVariables DefaultLocale, DocsRootDir {
    if (!(Get-Module platyPS -ListAvailable))
    {
        "platyPS module is not installed. Skipping $($psake.context.currentTaskName) task."
        return
    }

    $moduleInfo = Import-Module $ENV:BHPSModuleManifest -Global -Force -PassThru

    try
    {
        if ($moduleInfo.ExportedCommands.Count -eq 0)
        {
            "No commands have been exported. Skipping $($psake.context.currentTaskName) task."
            return
        }

        if (!(Test-Path -LiteralPath $DocsRootDir))
        {
            New-Item $DocsRootDir -ItemType Directory > $null
        }

        if (Get-ChildItem -LiteralPath $DocsRootDir -Filter *.md -Recurse)
        {
            Get-ChildItem -LiteralPath $DocsRootDir -Directory |
                ForEach-Object {
                Update-MarkdownHelp -Path $_.FullName -Verbose:$VerbosePreference > $null
            }
        }

        # ErrorAction set to SilentlyContinue so this command will not overwrite an existing MD file.
        New-MarkdownHelp -Module $ModuleName -Locale $DefaultLocale -OutputFolder (Join-Path $DocsRootDir $DefaultLocale) `
            -WithModulePage -ErrorAction SilentlyContinue -Verbose:$VerbosePreference > $null
    }
    finally
    {
        Remove-Module $ModuleName
    }
}


function MD5HashFile([string] $filePath)
{
    if ([string]::IsNullOrEmpty($filePath) -or !(Test-Path $filePath -PathType Leaf))
    {
        return $null
    }

    [System.IO.Stream] $file = $null;
    [System.Security.Cryptography.MD5] $md5 = $null;
    try
    {
        $md5 = [System.Security.Cryptography.MD5]::Create()
        $file = [System.IO.File]::OpenRead($filePath)
        return [System.BitConverter]::ToString($md5.ComputeHash($file))
    }
    finally
    {
        if ($null -ne $file)
        {
            $file.Dispose()
        }
    }
}

function Invoke-WithRetry
{
    param
    (
        [Parameter(Position = 0)]
        [scriptblock]$Script,

        [int]$Retries = 10
    )

    $count = 0
    $ex = $null

    while ($count++ -lt $Retries)
    {
        try
        {
            Invoke-Command -NoNewScope -ScriptBlock $Script
            return
        }
        catch
        {
            $ex = $_.Exception
            Write-Warning $ex.Message
            Start-Sleep -Seconds 1
        }
    }

    throw $ex
}
