function Wait-PSCFNStack
{
    <#
    .SYNOPSIS
        Wait for a stack to do something, printing events along the way.

    .PARAMETER StackArn
        Stack(s) to wait on.

    .PARAMETER CredentialArguments
        Hash of common credetial and region arguments.

    .PARAMETER StartTime
        Time the stack update command was issued

    .OUTPUTS
        [bool] true if operation succeeded; else false
#>
    param
    (
        [string[]]$StackArn,

        [hashtable]$CredentialArguments,

        [DateTime]$StartTime
    )

    $checkTime = $StartTime

    # Copy input array so as not to trash it.
    $arns = $StackArn |
    Foreach-Object {
        $_
    }

    # States indicating any stack operation complete
    $completionStates = @(
        'CREATE_COMPLETE'
        'CREATE_FAILED'
        'DELETE_COMPLETE'
        'DELETE_FAILED'
        'ROLLBACK_COMPLETE'
        'ROLLBACK_FAILED'
        'UPDATE_COMPLETE'
        'UPDATE_ROLLBACK_COMPLETE'
        'UPDATE_ROLLBACK_FAILED'
        'IMPORT_COMPLETE'
        'IMPORT_ROLLBACK_COMPLETE'
        'IMPORT_ROLLBACK_FAILED'
    )

    # Best guess how wide to make stack name column
    $stackColumnWidth = $arns | Foreach-Object {
        Get-CFNStack -StackName $_ @CredentialArguments
    } |
    Foreach-Object {
        $stack = $_

        # This stack
        $stack.StackName.Length

        # Nested stacks
        Get-CFNStackResourceList -StackName $_.StackId @CredentialArguments |
        Where-Object {
            $_.ResourceType -ieq 'AWS::CloudFormation::Stack'
        } |
        Foreach-Object {
            ($stack.StackName + "-" + $_.LogicalResourceId + "-" + ("X" * 12)).Length
        }
    } |
    Measure-Object -Maximum |
    Select-Object -ExpandProperty Maximum

    $anyFailed = $false
    $writeHeaders = $true

    # If localstack testing, poll faster
    $sleepTime = $(
        if ($CredentialArguments.Keys -icontains 'EndpointUrl')
        {
            1
        }
        else
        {
            10
        }
    )

    while (($arns | Measure-Object).Count -gt 0)
    {
        Start-Sleep -Seconds $sleepTime

        $stacks = $arns |
            Foreach-Object {

            Get-CFNStack -StackName $_ @CredentialArguments
        }

        $completedStackArns = $stacks |
            Where-Object {

            if ($_.StackStatus -ilike '*ROLLBACK*' -or $_.StackStatus -ilike '*FAILED*')
            {
                $anyFailed = $true
            }

            $completionStates -icontains $_.StackStatus
        } |
            Select-Object -ExpandProperty StackId

        if ($completedStackArns)
        {
            $arns = Compare-Object -ReferenceObject $arns -DifferenceObject $completedStackArns -PassThru
        }

        $ts = Write-StackEvents -StackArn $arns -EventsAfter $checkTime $CredentialArguments -WriteHeaders $writeHeaders -StackColumnWidth $stackColumnWidth
        $writeHeaders = $false

        if ($ts)
        {
            $checkTime = $ts
        }
    }

    # Capture return value so it doesn't leak into the pipeline
    $ts = Write-StackEvents -StackArn $StackArn -EventsAfter $checkTime $CredentialArguments -WriteHeaders $false -StackColumnWidth $stackColumnWidth

    # Output boolean - any final state containing ROLLBACK or FAILED indicates operation unsuccessful
    return (-not $anyFailed)
}