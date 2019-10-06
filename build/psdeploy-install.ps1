# Install custom PSDeploy until my PR gets merged.
$moduleLocation = 'https://ci.appveyor.com/api/buildjobs/be2ipkygtbbd9aa0/artifacts/psdeploy.zip'
$downloadedPackage = Join-Path ([IO.Path]::GetTempPath()) 'psdeploy.zip'

Write-Host "Downloading $moduleLocation"
Get-WebRequest -Uri $moduleLocation -OutFile $downloadedPackage


# Find local module dir
$moduleDir = $(

    if ((Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux)
    {
        "~/.local/share/powershell/Modules/PSDeploy/1.0.4"
    }
    elseif ($PSVersionTable.PSEdition -ieq 'Core')
    {
        # Core on Windows
        "$($env:HOME)\Documents\PowerShell\Modules\PSDeploy\1.0.4"
    }
    else
    {
        # Windows Powershell
        "$($env:HOME)\Documents\WindowsPowerShell\Modules\PSDeploy\1.0.4"
    }
)

if (-not (Test-Path -Path $moduleDir -PathType Container))
{
    New-Item -Path $moduleDir -ItemType Directory | Out-Null
}

# .NET compression libraries
'System.IO.Compression', 'System.IO.Compression.FileSystem' |
    Foreach-Object {
        [System.Reflection.Assembly]::LoadWithPartialName($_)
    }

Write-Host "Extracting to $moduleDir"

[System.IO.Compression.ZipFile]::ExtractToDirectory($downloadedPackage, $moduleDir)

