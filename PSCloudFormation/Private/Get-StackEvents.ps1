function Get-StackEvents
{
    <#
    .SYNOPSIS
        Recursively get stack events from parent and nested stacks

    .PARAMETER StackArn
        ARN(s) of stack(s) to get events for

    .OUTPUTS
        [Amazon.CloudFormation.Model.StackEvent[]]
        Array of stack failure events.
    #>
    param
    (
        [string[]]$StackArn,
        [hashtable]$CredentialArguments,
        [DateTime]$EventsAfter
    )

    $StackArn |
    Where-Object {
        $null -ne $_
    } |
    ForEach-Object {

        Get-CFNStackEvent -StackName $_ @CredentialArguments |
        Where-Object {
            $_.Timestamp -gt $EventsAfter
        }

        Get-CFNStackResourceList -StackName $_ @CredentialArguments |
        Where-Object {
            $_.ResourceType -ieq 'AWS::CloudFormation::Stack'
        } |
        ForEach-Object {

            if ($_ -and $_.PhysicalResourceId)
            {
                Get-StackEvents -StackArn $_.PhysicalResourceId -EventsAfter $EventsAfter -CredentialArguments $CredentialArguments
            }
        }
    }
}
