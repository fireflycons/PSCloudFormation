$windows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

if ($windows)
{
    Write-Host "Installing modules..."

    $prog = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'

    try
    {
        # Modules for doc building and deployment
        @(
            'psake'
            'BuildHelpers'
            'PSDeploy'
            'platyps'
        ) |
        ForEach-Object {
            Write-Host "-" $_
            Install-Module $_ -Scope CurrentUser -Force | Out-Null
        }
    }
    finally
    {
        $ProgressPreference = $prog
    }
}

Write-Host "Installing Terraform"

if ($windows)
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
