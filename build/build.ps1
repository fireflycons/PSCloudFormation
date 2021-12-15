param 
(
    [string]$Task = 'Default',
    [switch]$ImportDependenciesOnly
)

$currentLocation = Get-Location
try
{
    Set-Location $PSScriptRoot

    $cf = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object Location -Match "AWSSDK.Core.dll"

    if ($cf)
    {
        Write-Warning "A version of AWSSDK.Core.dll is already loaded: $($cf.Location)"
    }

    # Grab nuget bits, install modules, set build variables, start build.
    Write-Host 'Setting up build environment'

    Get-PackageProvider -Name NuGet -ForceBootstrap | Out-Null

    @(
        'psake'
        'BuildHelpers'
        'PSDeploy'
        'platyps'
    ) |
    ForEach-Object {
        Import-Module $_
    }

    Push-Location $(git rev-parse --show-toplevel)
    Set-BuildEnvironment -ErrorAction SilentlyContinue
    Pop-Location

    Invoke-psake -buildFile $PSScriptRoot/psake.ps1 -taskList $Task -nologo
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
