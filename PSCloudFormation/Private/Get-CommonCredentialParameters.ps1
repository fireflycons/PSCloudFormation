function Get-CommonCredentialParameters
{
    <#
        .SYNOPSIS
            Gets AWS Commmon Credential and Region parameters

        .PARAMETER CallerBoundParameters
            Value of $PSBoundParameters from the calling function

        .OUTPUTS
            [hashtable] Extracted Commmon Credential parameters for splatting AWSPowerShell calls
    #>

    param
    (
        [hashtable]$CallerBoundParameters
    )

    $credentialArgs = @{}
    $CallerBoundParameters.Keys |
        Where-Object { $Script:commonCredentialArguments.Keys -contains $_} |
        ForEach-Object {

        $credentialArgs.Add($_, $CallerBoundParameters[$_])
    }

    $credentialArgs
}
