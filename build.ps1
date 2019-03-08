param ($Task = 'Default')

$currentLocation = Get-Location
try
{
    Set-Location $PSScriptRoot

    # Grab nuget bits, install modules, set build variables, start build.
    Write-Host 'Setting up build environment'
    Get-PackageProvider -Name NuGet -ForceBootstrap | Out-Null

    $loadedModules = Get-Module | Select-Object -ExpandProperty Name
    $requiredModules = @(
        'Psake'
        'PSDeploy'
        'BuildHelpers'
        'platyPS'
        'Pester'
        'powershell-yaml'
    )

    # List of modules not already loaded
    $missingModules = Compare-Object -ReferenceObject $requiredModules -DifferenceObject $loadedModules |
    Where-Object {
        $_.SideIndicator -eq '<='
    } |
    Select-Object -ExpandProperty InputObject

    if ($missingModules)
    {
        $installedModules = Get-Module -ListAvailable | Select-Object -ExpandProperty Name

        $neededModules = $requiredModules |
            Where-Object {
                -not ($installedModules -icontains $_)
        }

        if (($neededModules | Measure-Object).Count -gt 0)
        {
            Write-Host "Installing modules: $($neededModules -join ',')"
            Install-Module $neededModules -Force -AllowClobber -SkipPublisherCheck -Scope CurrentUser
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

    # Make AppVeyor fail the build if this setup borks
    exit 1
}
finally
{
    Set-Location $currentLocation
}
