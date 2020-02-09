function Get-ExceptionForFailedStack
{
    param
    (
        [string]$StackArn,
        [hashtable]$CredentialArguments
    )

    $stack = Get-CFNStack -StackName $StackArn @CredentialArguments

    if ($stack.StackStatus.Value -like '*ROLLBACK*' -or $stack.StackStatus.Value -like '*FAILED*')
    {
        $operation = $stack.StackStatus.Value -split '_' | Select-Object -First 1
        $operation = $operation.Substring(0, 1) + $operation.Substring(1).ToLower()

        New-Object PSCloudFormation.Exceptions.CloudFormationException -ArgumentList ("$operation failed!", $StackArn, $stack.StackStatus)
    }
}