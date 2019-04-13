param ($Task = 'Default')

$currentLocation = Get-Location
try
{
    Set-Location $PSScriptRoot

    # Grab nuget bits, install modules, set build variables, start build.
    Write-Host 'Setting up build environment'
    Get-PackageProvider -Name NuGet -ForceBootstrap | Out-Null

    if (-not (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue))
    {
        $IsWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
    }

    if (-not (Get-Variable -Name IsCoreClr -ErrorAction SilentlyContinue))
    {
        $IsCoreClr = $PSVersionTable.ContainsKey('PSEdition') -and $PSVersionTable.PSEdition -eq 'Core'
    }

    $loadedModules = Get-Module | Select-Object Name, Version

    $sharedModules = @(
        New-Object PSObject -Property @{
            Name            = 'psake'
            RequiredVersion = [Version]'4.7.4'
        }
        New-Object PSObject -Property @{
            Name            = 'Pester'
            RequiredVersion = [Version]'4.7.3'
        }
        New-Object PSObject -Property @{
            Name            = 'BuildHelpers'
            RequiredVersion = [Version]'2.0.7'
        }
        New-Object PSObject -Property @{
            Name            = 'PSDeploy'
            RequiredVersion = [Version]'1.0.1'
        }
    )

    if ($IsWindows -and -not $IsCoreClr)
    {
        $requiredModules = Invoke-Command -NoNewScope {
            $sharedModules

            @(
                New-Object PSObject -Property @{
                    Name            = 'platyPS'
                    RequiredVersion = [Version]'0.12.0'
                }
                New-Object PSObject -Property @{
                    Name            = 'powershell-yaml'
                    RequiredVersion = [Version]'0.3.5'
                }
                New-Object PSObject -Property @{
                    Name            = 'AWSPowerShell'
                    RequiredVersion = [Version]'3.3.485.0'
                }
            )

        }
    }
    else
    {
        $requiredModules = Invoke-Command -NoNewScope {
            $sharedModules

            @(
                New-Object PSObject -Property @{
                    Name            = 'AWSPowerShell.netcore'
                    RequiredVersion = [Version]'3.3.485.0'
                }
            )
        }
    }

    # List of modules not already loaded
    $missingModules = Compare-Object -ReferenceObject $requiredModules.Name -DifferenceObject $loadedModules.Name |
    Where-Object {
        $_.SideIndicator -eq '<='
    } |
    Select-Object -ExpandProperty InputObject

    if ($missingModules)
    {
        $installedModules = Get-Module -ListAvailable |
        Select-Object Name, Version

        $neededModules = $requiredModules |
        Where-Object {
            $r = $_
            -not ($installedModules | Where-Object {
                    $_.Name -eq $r.Name -and $_.Version -ge $r.RequiredVersion
                })
        }

        $neededModules |
        ForEach-Object {
            Write-Host "Installing module: $($_.Name) $($_.RequiredVersion)"
            Install-Module -Name $_.Name -RequiredVersion $_.RequiredVersion -Force -AllowClobber -SkipPublisherCheck -Scope CurrentUser
        }

        Write-Host "Importing modules: $($missingModules -join ',')"
        Import-Module $missingModules
    }

    Set-BuildEnvironment -ErrorAction SilentlyContinue

    if (${env:BHBuildSystem} -ine 'Unknown')
    {
        # Seems AppVeyor's version of this is quite out of date
        # These commands are not found: Wait-CFNStack, New-CFNChangeSet
        Install-Module AWSPowerShell -Force -AllowClobber -SkipPublisherCheck -Scope CurrentUser
    }

    Invoke-psake -buildFile $ENV:BHProjectPath\psake.ps1 -taskList $Task -nologo
    exit ( [int]( -not $psake.build_success ) )
}
catch
{
    Write-Error $_.Exception.Message
    Write-Error $_.ScriptStackTrace
    # Make AppVeyor fail the build if this setup borks
    exit 1
}
finally
{
    Set-Location $currentLocation
}
