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

Task BuildAppVeyor -Depends Build, Deploy

Task Init {
    $lines
    Set-Location $ProjectRoot
    "Build System Details:"
    Get-Item ENV:BH*
    "`n"

    if ($script:IsLinux)
    {
        throw "Test fast_finish"
    }

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

    Get-Module | Out-Host
}

Task Test -Depends ListModules {
    $lines
    "`n`tSTATUS: Testing with PowerShell $PSVersion"

    # Gather test results. Store them in a variable and file
    $pesterParameters = @{
        Path         = Join-Path $ProjectRoot tests
        PassThru     = $true
        OutputFormat = "NUnitXml"
        OutputFile   = Join-Path $ProjectRoot $TestFile
    }

    if (-Not $IsWindows) { $pesterParameters["ExcludeTag"] = "WindowsOnly" }
    $TestResults = Invoke-Pester @pesterParameters

    # In Appveyor?  Upload our tests! #Abstract this into a function?
    If ($ENV:BHBuildSystem -eq 'AppVeyor')
    {
        (New-Object 'System.Net.WebClient').UploadFile(
            "https://ci.appveyor.com/api/testresults/nunit/$($env:APPVEYOR_JOB_ID)",
            (Join-Path $ProjectRoot $TestFile) )
    }

    Remove-Item (Join-Path $ProjectRoot $TestFile) -Force -ErrorAction SilentlyContinue

    # Failed tests?
    # Need to tell psake or it will proceed to the deployment. Danger!
    if ($TestResults.FailedCount -gt 0)
    {
        Write-Error "Failed '$($TestResults.FailedCount)' tests, build failed"
    }
    "`n"
}

Task Build -Depends Test {
    $lines

    # Load the module, read the exported functions, update the psd1 FunctionsToExport
    #Set-ModuleFunctions

    # Bump the module version if we didn't already
    Try
    {
        [version]$GalleryVersion = Get-NextNugetPackageVersion -Name $env:BHProjectName -ErrorAction Stop
        [version]$GithubVersion = Get-MetaData -Path $env:BHPSModuleManifest -PropertyName ModuleVersion -ErrorAction Stop
        if ($GalleryVersion -ge $GithubVersion)
        {
            Update-Metadata -Path $env:BHPSModuleManifest -PropertyName ModuleVersion -Value $GalleryVersion -ErrorAction stop
        }
    }
    Catch
    {
        "Failed to update version for '$env:BHProjectName': $_.`nContinuing with existing version"
    }
}

Task Deploy {
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

Task BuildHelp -Depends Build, GenerateMarkdown, GenerateHelpFiles {}

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

Task GenerateHelpFiles -requiredVariables DocsRootDir, ModuleName, ModuleOutDir {
    if (!(Get-Module platyPS -ListAvailable))
    {
        "platyPS module is not installed. Skipping $($psake.context.currentTaskName) task."
        return
    }

    if (!(Get-ChildItem -LiteralPath $DocsRootDir -Filter *.md -Recurse -ErrorAction SilentlyContinue))
    {
        "No markdown help files to process. Skipping $($psake.context.currentTaskName) task."
        return
    }

    $helpLocales = (Get-ChildItem -Path $DocsRootDir -Directory).Name

    # Generate the module's primary MAML help file.
    foreach ($locale in $helpLocales)
    {
        New-ExternalHelp -Path (Join-Path $DocsRootDir $locale) -OutputPath (Join-Path $ModuleOutDir $locale) -Force `
            -ErrorAction SilentlyContinue -Verbose:$VerbosePreference > $null
    }
}
