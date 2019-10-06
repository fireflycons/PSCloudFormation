# Install custom PSDeploy until my PR gets merged.
$moduleLocation = 'https://ci.appveyor.com/api/buildjobs/be2ipkygtbbd9aa0/artifacts/psdeploy.zip'
$downloadedPackage = Join-Path ([IO.Path]::GetTempPath()) 'psdeploy.zip'

Write-Host "Downloading $moduleLocation"
Invoke-WebRequest -UseBasicParsing -Uri $moduleLocation -OutFile $downloadedPackage


# Find local module dir
$moduleDir = $(

    if ((Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux)
    {
        $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('~/.local/share/powershell/Modules/PSDeploy/1.0.4')
    }
    elseif ($PSVersionTable.PSEdition -ieq 'Core')
    {
        # Core on Windows
        "$($env:USERPROFILE)\Documents\PowerShell\Modules\PSDeploy\1.0.4"
    }
    else
    {
        # Windows Powershell
        "$($env:USERPROFILE)\Documents\WindowsPowerShell\Modules\PSDeploy\1.0.4"
    }
)

if (-not (Test-Path -Path $moduleDir -PathType Container))
{
    New-Item -Path $moduleDir -ItemType Directory | Out-Null
}

# .NET compression libraries
'System.IO.Compression', 'System.IO.Compression.FileSystem' |
    Foreach-Object {
        [System.Reflection.Assembly]::LoadWithPartialName($_) | Out-Null
    }

Write-Host "Extracting to $moduleDir"

[System.IO.Compression.ZipFile]::ExtractToDirectory($downloadedPackage, $moduleDir)

Write-Host "Checking installation"

if (-not (Get-Module -ListAvailable PSDeploy))
{
    throw "Installation unsuccessful"
}