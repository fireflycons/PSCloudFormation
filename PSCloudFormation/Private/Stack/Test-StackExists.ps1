function Test-StackExists
{
    <#
    .SYNOPSIS
        Tests whether a stack exists.

    .PARAMETER StackName
        Stack to test

    .PARAMETER CredentialArguments
        Any common creadential arguments given to caller

    .OUTPUTS
        [bool] true if stack exists; else false
    #>
    param
    (
        [string]$StackName,
        [hashtable]$CredentialArguments
    )

    try
    {
        Get-CFNStack -StackName $StackName @CredentialArguments
        $true
    }
    catch
    {
        $false
    }
}
