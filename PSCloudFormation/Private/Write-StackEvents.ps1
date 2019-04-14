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

    .PARAMETER StackColumnWidth
        Max width of stack name column

    .OUTPUTS
        Timestamp of most recent event
    #>
    param
    (
        [string[]]$StackArn,
        [hashtable]$CredentialArguments,
        [DateTime]$EventsAfter,
        [bool]$WriteHeaders,
        [int]$StackColumnWidth
    )

    function Format-FixedWidthString
    {
        param
        (
            [string]$Text,
            [int]$Maxlen
        )

        if ($Text.Length -gt $MaxLen)
        {
            $Text.Substring(0, $Maxlen - 3) + "..."
        }
        else
        {
            $Text.PadRight($MaxLen)
        }
    }

    $writePSObjectArgs = @{
        Column          = @('Status', 'Status', 'Status')
        MatchMethod     = @('Query', 'Query', 'Query')
        Value           = @("'Status' -match 'COMPLETE\s*$'", "'Status' -match 'PROGRESS\s*$'", "'Status' -match 'FAILED\s*$'")
        ValueForeColor  = @('Green', 'Yellow', 'Red')
        BodyOnly        = -not $WriteHeaders
    }

    try {
        $events = Get-StackEvents -StackArn $StackArn -EventsAfter $EventsAfter -CredentialArguments $CredentialArguments
        $events |
        Sort-Object Timestamp |
        ForEach-Object {
            # Issue #15 - Create a new object of fixed width strings
            [PSCustomObject][ordered]@{
                TimeStamp       = $_.Timestamp.ToString("HH:mm:ss")
                StackName       = Format-FixedWidthString -Text $_.StackName -Maxlen $StackColumnWidth
                'Logical ID'    = Format-FixedWidthString -Text $_.LogicalResourceId -Maxlen 40
                Status          = Format-FixedWidthString -Text $_.ResourceStatus.Value -Maxlen 45
                'Status Reason' = $(
                    if ([string]::IsNullOrEmpty($_.ResourceStatusReason))
                    {
                        "-"
                    }
                    else
                    {
                        $_.ResourceStatusReason
                    }
                )
            }
        } |
        Write-PSObject @writePSObjectArgs
        }
    catch {
        $_.ScriptStackTrace
        throw
    }

    $events.Timestamp |
    Measure-Object -Maximum |
    Select-Object -ExpandProperty Maximum
}
