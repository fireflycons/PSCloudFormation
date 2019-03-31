function Write-StackEvents
{
    <#
    .SYNOPSIS
        Writes stack events to the console

    .DESCRIPTION
        Gets events for a stack and any nested stacks from
        the given start time till present time.

    .PARAMETER StackArn
        ARNs of stacks to write events for

    .PARAMETER EventsAfter
        Get all events from this UTC time till now

    .PARAMETER CredentialArguments
        Hash of AWS common credential and region arguments

    .PARAMETER WriteHeaders
        If true, write column headers

    .OUTPUTS
        Timestamp of most recent event
    #>
    param
    (
        [string[]]$StackArn,
        [hashtable]$CredentialArguments,
        [DateTime]$EventsAfter,
        [bool]$WriteHeaders
    )

    $writePSObjectArgs = @{
        Column          = @('ResourceStatus', 'ResourceStatus', 'ResourceStatus')
        MatchMethod     = @('Query', 'Query')
        Value           = @("'ResourceStatus' -like '*COMPLETE'", "'ResourceStatus' -like '*PROGRESS'", "'ResourceStatus' -like '*FAILED'")
        BodyOnly        = -not $WriteHeaders
        ValueForeColor  = @('Green', 'Yellow', 'Red')
        ColoredColumns  = 'StackName'
        ColumnForeColor = 'Magenta'
    }

    $events = Get-StackEvents -StackArn $StackArn -EventsAfter $EventsAfter -CredentialArguments $CredentialArguments
    $events |
        Select-Object Timestamp, StackName, LogicalResourceId, ResourceStatus, ResourceStatusReason |
        Sort-Object Timestamp |
        Write-PSObject @writePSObjectArgs

    $events.Timestamp |
        Measure-Object -Maximum |
        Select-Object -ExpandProperty Maximum
}
