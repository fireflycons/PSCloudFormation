Write-Host "Installing Terraform"

if ((-not (Get-Variable -Name IsWindows -ErrorAction Ignore)) -or $IsWindows)
{
    # Windows
    $cinst = Get-Command -Name cinst -ErrorAction SilentlyContinue
    if (-not $cinst)
    {
        Write-Host "Chocolatey not present on this platform. Cannot continue"
        exit 1
    }

    & $cinst terraform --yes --limit-output |
    Foreach-Object {
        if ($_ -inotlike 'Progress*Saving*')
        {
            Write-Host $_
        }
    }
}
else
{
    # Linux
    $installer = Get-Command (Join-Path $PSScriptRoot 'install-terraform.sh')
    & $installer
}

exit $LASTEXITCODE
