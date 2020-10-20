
if ($PSEdition -eq 'Core')
{
    Write-Host "Publish docs step not in scope in this environment"
    return
}

# Dot-source vars describing environment
. (Join-Path $PSScriptRoot build-environment.ps1)

# TODO - Only if master and repo_tag
if (-not $isAppVeyor)
{
    Write-Host "Publish docs step not in scope in this environment"
    return
}

if (-not ($canPublishDocs -and ($isReleasePublication -or $forceDocPush)))
{
    Write-Host "Cannot publish docs in forked repo or if not publishing a release"
    return
}

# Dot-source git helper
. (Join-Path $PSScriptRoot Invoke-Git.ps1)

Push-Location (Join-Path $env:APPVEYOR_BUILD_FOLDER "../fireflycons.github.io")

try
{
    # Stage any changes, suppressing line ending warnings (nearly all at least)
    Invoke-Git -SuppressWarnings add --all

    # Check status
    $stat = Invoke-Git -OutputToPipeline status --short

    if ($null -eq $stat)
    {
        Write-Host "No overall changes to documentation detected - nothing to push."
        return
    }

    $stat |
    ForEach-Object {
        Write-Host $_
    }

    # Commit
    Invoke-Git commit -m "AppVeyor Build ${env:APPVEYOR_BUILD_NUMBER}"

    # and push
    Invoke-Git push

    Write-Host
    Write-Host "Documentation changes pushed."
}
finally
{
    Pop-Location
}