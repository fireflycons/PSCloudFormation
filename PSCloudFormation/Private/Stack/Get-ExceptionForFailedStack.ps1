function Get-ExceptionForFailedStack
{
    param
    (
        [string]$StackArn,

        [ValidateSet('Create', 'Update', 'Delete')]
        [string]$Operation,
        [hashtable]$CredentialArguments
    )

    $stack = Get-CFNStack -StackName $StackArn @CredentialArguments

    if ($stack.StackStatus.Value -like '*ROLLBACK*' -or $stack.StackStatus.Value -like '*FAILED*')
    {
        New-Object PSCloudFormation.Exceptions.CloudFormationException -ArgumentList ("$Operation stack failed! Final state: $($stack.StackStatus)", $StackArn, $stack.StackStatus)
    }
}