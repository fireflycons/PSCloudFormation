function Get-StackFailureEvents
{
    <#
    .SYNOPSIS
        Gets failure event list from a briken stack

    .DESCRIPTION
        Gets failure events for a failed stack and also attempts
        to get the events for any nested stack. This depends on
        the functionbeing able to get to the nested stack resource
        before AWS removes it.

    .PARAMETER StackName
        Name of failed stack

    .OUTPUTS
        [Amazon.CloudFormation.Model.StackEvent[]]
        Array of stack failure events.
    #>
    param
    (
        [string]$StackName,
        [hashtable]$CredentialArguments
    )

    Get-CFNStackEvent -StackName $StackName @CredentialArguments |
        Where-Object {
        $_.ResourceStatus -ilike '*FAILED*' -or $_.ResourceStatus -ilike '*ROLLBACK*'
    }

    Get-CFNStackResourceList -StackName $StackName @CredentialArguments |
        Where-Object {
        $_.ResourceType -ieq 'AWS::CloudFormation::Stack'
    } |
        ForEach-Object {

        if ($_ -and $_.PhysicalResourceId)
        {
            Get-StackFailureEvents -StackName $_.PhysicalResourceId -CredentialArguments $CredentialArguments
        }
    }
}
