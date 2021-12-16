$windows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

if ($windows)
{
    Write-Host "Installing modules..."


    $prog = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'

    $modules = @{
        psake = '4.9.0'
        BuildHelpers = $null
        PSDeploy = '1.0.5'
        platyps = '0.14.2'
        'AWS.Tools.CloudFormation' = '4.1.16.0'
        'AWS.Tools.S3' = '4.1.16.0'
    }

    try
    {
        # Modules for doc building and deployment
        $modules.Keys |
        ForEach-Object {
            $n = $_
            $v = $modules[$_]

            if ($null -eq $v)
            {
                Write-Host "-" $n "latest"
                Install-Module $_ -Scope CurrentUser -Force -AllowClobber | Out-Null
            }
            else
            {
                Write-Host "-" $n $v
                Install-Module $_ -RequiredVersion $v -Scope CurrentUser -Force -AllowClobber | Out-Null
            }
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
