# Dot-source vars describing environment
. (Join-Path $PSScriptRoot build-environment.ps1)


if ($isAppVeyor)
{
    # Filter out netcore 5 preview and select latest netcore 3.1
    $net31latest = dotnet --info |
    Foreach-Object {
        if ($_ -match '^\s+(3\.1\.\d+)')
        {
            [Version]$Matches.0 }
        } |
        Sort-Object -Descending |
        Select-Object -first 1

    if (-not $net31latest)
    {
        throw "Cannot find .NET core 3.1 SDK"
    }

    $globalJson = @{
        sdk = @{
            version = $net31latest.ToString()
        }
    } |
    ConvertTo-Json

    Write-Host "Setting build SDK to .NET core $net31latest"
    Invoke-Command -NoNewScope {
        Get-ChildItem -Path (Join-Path $PSScriptRoot '..') -Filter *.csproj -Recurse
        Get-ChildItem -Path (Join-Path $PSScriptRoot '..') -Filter *.sln -Recurse
    } |
    Foreach-Object {

        $filename = Join-Path $_.DirectoryName 'global.json'
        [System.IO.File]::WriteAllText($filename, $globalJson, [System.Text.Encoding]::ASCII)
        Write-Host "- Wrote $filename"
    }
}
