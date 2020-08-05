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

    if (-not (Get-Module -ListAvailable PSDepend))
    {
        Install-Module PSDepend -Repository PSGallery -Scope CurrentUser -Force
    }

    Import-Module PSDepend

    $psDependTags = $(
        if (Test-Path -Path variable:PSEdition)
        {
            $PSEdition
        }
        else
        {
            'Desktop'
        }
    )

    if ($ImportDependenciesOnly)
    {
        Invoke-PSDepend -Path "$PSScriptRoot\build.requirements.psd1" -Import -Force -Tags $psDependTags
    }
    else
    {
        Invoke-PSDepend -Path "$PSScriptRoot\build.requirements.psd1" -Install -Import -Force -Tags $psDependTags
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
