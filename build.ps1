param ($Task = 'Default')

$currentLocation = Get-Location
try
{
    Set-Location $PSScriptRoot

    # Grab nuget bits, install modules, set build variables, start build.
    Get-PackageProvider -Name NuGet -ForceBootstrap | Out-Null

    Install-Module Psake, PSDeploy, BuildHelpers, platyPS -force -AllowClobber -Scope CurrentUser
    Install-Module Pester -MinimumVersion 4.1 -Force -AllowClobber -SkipPublisherCheck -Scope CurrentUser
    Import-Module Psake, BuildHelpers, platyPS
    Set-BuildEnvironment -ErrorAction SilentlyContinue

    if (${env:BHBuildSystem} -ine 'Unknown')
    {
        # Seems AppVeyor's version of this is quite out of date
        # These commands are not found: Wait-CFNStack, New-CFNChangeSet
        Install-Module AWSPowerShell -Force -AllowClobber -SkipPublisherCheck -Scope CurrentUser
    }

    if (-not (Get-Module powershell-yaml -ErrorAction SilentlyContinue))
    {
        Install-Module powershell-yaml -Force -AllowClobber -SkipPublisherCheck -Scope CurrentUser
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
