param ($Task = 'Default')

$currentLocation = Get-Location
try
{
    Set-Location $PSScriptRoot
    
    # Grab nuget bits, install modules, set build variables, start build.
    Get-PackageProvider -Name NuGet -ForceBootstrap | Out-Null

    Install-Module Psake, PSDeploy, BuildHelpers -force -AllowClobber -Scope CurrentUser
    Install-Module Pester -MinimumVersion 4.1 -Force -AllowClobber -SkipPublisherCheck -Scope CurrentUser
    Import-Module Psake, BuildHelpers

    Set-BuildEnvironment -ErrorAction SilentlyContinue

    Invoke-psake -buildFile $ENV:BHProjectPath\psake.ps1 -taskList $Task -nologo
    exit ( [int]( -not $psake.build_success ) )
}
finally
{
    Set-Location $currentLocation
}
