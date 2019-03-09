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
    else
    {
        [Amazon.Runtime.FallbackRegionFactory]::GetRegionEndpoint().SystemName
    }
}