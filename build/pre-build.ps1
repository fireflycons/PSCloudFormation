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

if ($PSEdition -eq 'Core')
{
    Write-Host "Publish docs step not in scope in this environment"
    return
}


# TODO - Only if master and repo_tag
if (-not $isAppVeyor)
{
    Write-Host "Publish docs step not in scope in this environment"
    return
}

if (-not ($isReleasePublication -or $forceDocPush))
{
    Write-Host "Not cloning documentation site as this isn't a publish build"
    return
}

Write-Host "Cloning documention site"

# Dot-source git helper
. (Join-Path $PSScriptRoot Invoke-Git.ps1)

# https://www.appveyor.com/docs/how-to/git-push/

$cloneFolder = Join-Path $env:APPVEYOR_BUILD_FOLDER '..'

Invoke-Git config --global credential.helper store
Add-Content "$HOME\.git-credentials" "https://${env:GITHUB_PAT}:x-oauth-basic@github.com`n"
Invoke-Git config --global user.email "${env:GITHUB_EMAIL}"
Invoke-Git config --global user.name "AppVeyor"
Push-Location $cloneFolder
Invoke-Git clone https://github.com/fireflycons/fireflycons.github.io.git
Pop-Location

