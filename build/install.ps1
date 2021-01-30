# Dot-source vars describing environment
. (Join-Path $PSScriptRoot build-environment.ps1)

# First, install refasmer for reference assembly creation
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue

if (-not $dotnet)
{
    Write-Host -ForegroundColor Red "dotnet executable not found. Can't continue!"
    exit 1
}

& $dotnet tool install -g JetBrains.Refasmer.CliTool


# Set up DocFX
$cinst = Get-Command -Name cinst -ErrorAction SilentlyContinue
if (-not $cinst)
{
    Write-Host "Chocolatey not present on this platform. DocFX install skipped"
    return
}

& $cinst docfx --yes --limit-output |
Foreach-Object {
    if ($_ -inotlike 'Progress*Saving*')
    {
        Write-Host $_
    }
}
exit $LASTEXITCODE
