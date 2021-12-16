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
        $script:IsWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
        $script:IsLinux = (Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux
        $script:IsMacOS = (Get-Variable -Name IsMacOS -ErrorAction Ignore) -and $IsMacOS
        $script:IsCoreCLR = $PSVersionTable.ContainsKey('PSEdition') -and $PSVersionTable.PSEdition -eq 'Core'
    }
    catch { }

    $Timestamp = Get-date -uformat "%Y%m%d-%H%M%S"
    $PSVersion = $PSVersionTable.PSVersion.Major
    $lines = '----------------------------------------------------------------------'

    $Verbose = @{}
    if ($ENV:BHCommitMessage -match "!verbose")
    {
        $Verbose = @{Verbose = $True}
    }

    $DefaultLocale = 'en-US'
    $CmdletDocsOutputDir= Join-Path $env:BHProjectPath 'docfx/cmdlets'
    $ModuleName = "PSCloudFormation"

    $DocFxDirectory = (Resolve-Path (Join-Path $PSScriptRoot ../docfx)).Path

    # Dot-source vars describing environment
    . (Join-Path $PSScriptRoot build-environment.ps1)
}

Task Default -Depends BuildAppVeyor, Deploy


Task Init {
    $lines
    [Reflection.Assembly]::LoadWithPartialName("System.Security") | Out-Null
    Set-Location $ProjectRoot

    # Update path to module manifest
    #$modulePath = (Resolve-Path (Get-Content -Raw (Join-Path $env:BHProjectPath ModulePath.txt)).Trim()).Path
    #New-Item -Path Env:\ -Name BHPSModuleManifest -Value (Get-ChildItem -Path $modulePath -Filter *.psd1 | Select-Object -ExpandProperty FullName) -Force | Out-Null

    "Build System Details:"
    Get-ChildItem ENV: | Where-Object { $_.Name -like 'BH*' -or $_.Name -like 'AWS_TOOLS*' -or $_.Name -like 'PSCFN_*' }
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

Task UpdateManifest -Depends Init {

    # Load the module, read the exported commands, update the psd1 CmdletsToExport
    try
    {
        $params = @{
            Name = $env:BHPSModuleManifest
        }

        if (-not (Test-Path -Path $env:BHPSModuleManifest))
        {
            throw "Could not find module '$env:BHPSModuleManifest'"
        }

        # Create a runspace, add script to run
        $PowerShell = [Powershell]::Create()
        [void]$PowerShell.AddScript({
            Param
            (
                $Name
            )
            try
            {
                Import-Module -Name $Name -PassThru -Force
            }
            catch
            {
                [Console]::WriteLine($_.Exception.Message)
                $null
            }
            #$module = Import-Module -Name $Name -PassThru -Force
            #$module | Where-Object {$_.Path -notin $module.Scripts}
        }).AddParameters($params)

        $Module = $PowerShell.Invoke()

        if ($PowerShell.HadErrors)
        {
            throw $PowerShell.Streams.Error[0].Exception
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
    Update-Metadata -Path $env:BHPSModuleManifest -PropertyName ModuleVersion -Value $env:PSCFN_ModuleVersion -ErrorAction stop

    # Set file list
    $excludeList = Invoke-Command -NoNewScope {
        Get-ExcludedFiles | Select-Object -ExpandProperty name
        '*.psd1'
    }

    Update-Metadata -Path $env:BHPSModuleManifest -PropertyName FileList -Value (Get-ChildItem -Path (Split-Path -Parent $env:BHPSModuleManifest) -Exclude $excludeList).Name
}

Task CleanModule {

    Get-ExcludedFiles |
    Foreach-Object {

        $recurse = @{}

        if (Test-Path -Path $_.FullName -PathType Container)
        {
            $recurse.Add('Recurse', $true)
        }

        Remove-Item $_.FullName @recurse
    }
}

Task Deploy -Depends Init, CleanModule {

    $lines

    Write-Host "Testing module manifest"
    Test-ModuleManifest -Path $env:BHPSModuleManifest | Out-Null

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

Task BuildAppVeyor -Depends Init, UpdateManifest, GenerateMarkdown {}

Task GenerateMarkdown -requiredVariables DefaultLocale, CmdletDocsOutputDir {
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

        if (!(Test-Path -LiteralPath $CmdletDocsOutputDir))
        {
            New-Item $CmdletDocsOutputDir -ItemType Directory > $null
        }

        if (Get-ChildItem -LiteralPath $CmdletDocsOutputDir -Filter *.md -Recurse)
        {
            Get-ChildItem -LiteralPath $CmdletDocsOutputDir -Directory |
                ForEach-Object {
                Update-MarkdownHelp -Path $_.FullName -Verbose:$VerbosePreference > $null
            }
        }

        # ErrorAction set to SilentlyContinue so this command will not overwrite an existing MD file.
        New-MarkdownHelp -Module $ModuleName -Locale $DefaultLocale -OutputFolder $CmdletDocsOutputDir `
            -WithModulePage -ErrorAction SilentlyContinue -Verbose:$VerbosePreference > $null

        $toc = New-Object System.Text.StringBuilder

        # Post process the generated markdown to add DFM YAML headers where not present
        Get-ChildItem -Path $CmdletDocsOutputDir -Filter *.md |
        Where-Object {
            $_.Name -inotlike 'index*'
        } |
        Sort-Object Name |
        Foreach-Object {

            $fileName = [IO.Path]::GetFileNameWithoutExtension($_.Name)
            $header = @{}

            $headerFound = $false
            foreach($line in (Get-Content $_.FullName))
            {
                if ($line -eq '---')
                {
                    if (-not $headerFound)
                    {
                        $headerFound = $true
                        continue
                    }

                    # Got header now
                    break
                }

                if ($headerFound -and $line -match '^(?<key>[\w\s]+):\s+(?<value>.*)')
                {
                    $header.Add($Matches.key, $matches.value)
                }
            }

            # Add DFM keys
            $header['uid'] = $fileName
            $header['title'] = $filename

            # Render header back to YAML
            $sb = New-Object System.Text.StringBuilder
            $sb.AppendLine('---') | Out-Null
            $header.Keys |
            ForEach-Object {
                $sb.AppendLine("$($_): $($header[$_])") | Out-Null
            }
            $sb.AppendLine('---') | Out-Null

            $content = Get-Content -Raw $_.FullName

            if ($headerFound)
            {
                # Regex replace the new header
                $content = [System.Text.RegularExpressions.Regex]::Replace($content, '^---(\r\n|\r|\n).*?(\r\n|\r|\n)---', $sb.ToString(), [System.Text.RegularExpressions.RegexOptions]::Singleline)
            }
            else
            {
                # Prepend new header
                $content = $sb.ToString() + $content
            }

            # Write out modified file (UTF8 No BOM)
            [IO.File]::WriteAllText($_.FullName, $content, [System.Text.UTF8Encoding]::new($false))

            # Update TOC
            if ($_.Name -like "$($ENV:BHProjectName)*")
            {
                $toc.AppendLine("- name: Module Index").AppendLine("  href: $($_.Name)") | Out-Null
            }
            else
            {
                $toc.AppendLine("- name: $filename").AppendLine("  href: $($_.Name)") | Out-Null
            }

        }

        # Write TOC file
        [IO.File]::WriteAllText((Join-Path $CmdletDocsOutputDir toc.yml), $toc.ToString(), [System.Text.UTF8Encoding]::new($false))
    }
    finally
    {
        Remove-Module $ModuleName
    }
}

function Get-ExcludedFiles
{
    try
    {
        Push-Location (Split-Path -Parent $env:BHPSModuleManifest)

        Invoke-Command -NoNewScope {
            Get-ChildItem -Filter AWS*
            Get-ChildItem -Filter *.pdb
            Get-ChildItem -Filter *.json
            Get-Item publish -ErrorAction SilentlyContinue
            Get-Item debug.ps1 -ErrorAction SilentlyContinue
            Get-Item Firefly.PSCloudFormation.xml -ErrorAction SilentlyContinue
            Get-Item System.Management.Automation.dll -ErrorAction SilentlyContinue
        } |
        Where-Object {
            $null -ne $_ -and (Test-Path -Path $_.FullName)
        }
    }
    finally
    {
        Pop-Location
    }
}