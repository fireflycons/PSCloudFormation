function Get-CurrentRegion
{
<#
    .SYNOPSIS
        Determine region from command line arguments or AWS default.

    .PARAMETER CredentialArguments
        Credential arguments passed to public function.

    .OUTPUTS
        [string] Region name.
#>
    param
    (
        [hashtable]$CredentialArguments
    )

    if ($CredentialArguments -and $CredentialArguments.ContainsKey('Region'))
    {
        $CredentialArguments['Region']
    }
    elseif (Test-Path -Path variable:StoredAWSRegion)
    {
        $StoredAWSRegion
    }
    else
    {
        $fallbackRegion = [Amazon.Runtime.FallbackRegionFactory]::GetRegionEndpoint()

        if ($null -ne $fallbackRegion)
        {
            $fallbackRegion.SystemName
        }
        else
        {
            throw "Cannot determine AWS Region. Either use Set-DefaultAWSRegion to set in shell, or supply -Region parameter."
        }
    }
}