function Remove-PSCFNStack
{
    <#
    .SYNOPSIS
        Delete one or more stacks.

    .DESCRIPTION
        Delete one or more stacks.
        If -Wait is specified, stack events are output to the console including events from any nested stacks.

        Deletion of multiple stacks can be either sequential or parallel.
        If deleting a gruop of stacks where there are dependencies between them
        use the -Sequential switch and list the stacks in dependency order.

    .PARAMETER StackName
        Either stack names or the object returned by Get-CFNStack, New-CFNStack, Update-CFNStack
        and other functions in this module when run with -Wait.

    .PARAMETER Wait
        If set and -Sequential is not set (so deleting in parallel), wait for all stacks to be deleted before returning.

    .PARAMETER Force
        If set, do not ask first.

    .PARAMETER Sequential
        If set, delete stacks in the order they are specified on the command line or received from the pipeline,
        waiting for each stack to delete successfully before proceeding to the next one.

    .PARAMETER BackupTemplate
        If set, back up the current version of the template stored by CloudFormation, along with the current parameter set if any to files in the current directory. This will assist with undoing any unwanted change.
        Note that if you have dropped or replaced a database or anything else associcated with stored data, then the data is lost forever!

    .INPUTS
        System.String[]
            You can pipe the names or ARNs of the stacks to delete to this function

    .OUTPUTS
        System.String[]
            ARN(s) of deleted stack(s) else nothing if the stack did not exist or no stacks were deleted.

    .EXAMPLE

        Remove-PSCFNStack -StackName MyStack

        Deletes a single stack.

    .EXAMPLE

        Remove-PSCFNStack -StackName MyStack -BackupTemplate

        As per the first example, but with the previous version of the template and its current parameter set saved to files in the current directory.

    .EXAMPLE

        'DependentStack', 'BaseStack' | Remove-PSCFNStack -Sequential

        Deletes 'DependentStack', waits for completion, then deletes 'BaseStack'.

    .EXAMPLE

        'Stack1', 'Stack2' | Remove-PSCFNStack -Wait

        Sets both stacks deleting in parallel, then waits for them both to complete.

    .EXAMPLE

        'Stack1', 'Stack2' | Remove-PSCFNStack

        Sets both stacks deleting in parallel, and returns immediately.
        See the CloudFormation console to monitor progress.

    .EXAMPLE

        Get-CFNStack | Remove-PSCFNStack

        You would NOT want to do this, just like you wouldn't do rm -rf / ! It is for illustration only.
        Sets ALL stacks in the region deleting simultaneously, which would probably trash some stacks
        and then others would fail due to dependent resources.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [string[]]$StackName,

        [switch]$Wait,

        [switch]$Sequential,

        [switch]$Force,

        [switch]$BackupTemplate
    )

    DynamicParam
    {
        #Create the RuntimeDefinedParameterDictionary
        New-Object System.Management.Automation.RuntimeDefinedParameterDictionary |
            New-CredentialDynamicParameters
    }

    begin
    {
        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters
        $deleteErrors = $false
    }

    process
    {
        # Issue #14 - Get-CFNStackEvents returns timestamp in local time
        $startTime = [DateTime]::Now

        $arns = $StackName |
            ForEach-Object {

            $cancelled = $false

            if (Test-StackExists -StackName $_ -CredentialArguments $credentialArguments)
            {
                $arn = (Get-CFNStack -StackName $_ @credentialArguments).StackId

                if ($BackupTemplate)
                {
                    Save-TemplateBackup -StackName $arn -CredentialArguments $credentialArguments -OutputPath (Get-Location).Path
                }

                if (-not $Force)
                {
                    $choice = $host.ui.PromptForChoice(
                        "Delete $StackName now?",
                        $null,
                        @(
                            New-Object System.Management.Automation.Host.ChoiceDescription ('&Yes', "Delete now." )
                            New-Object System.Management.Automation.Host.ChoiceDescription ('&No', 'Cancel operation.')
                        ),
                        1
                    )

                    if ($choice -ne 0)
                    {
                        Write-Warning "Delete $_ cancelled."
                        $cancelled = $true
                    }
                }

                if (-not $cancelled)
                {
                    Remove-CFNStack -StackName $arn -Force @credentialArguments

                    if ($Sequential -or ($Wait -and ($StackName | Measure-Object).Count -eq 1))
                    {
                        # Wait for this delete to complete before starting the next
                        Write-Host "Waiting for delete: $arn"

                        if (-not (Wait-PSCFNStack -StackArn $arn -CredentialArguments $credentialArguments -StartTime $startTime))
                        {
                            Write-Warning "Delete $_ unsuccessful"
                            $deleteErrors = $true
                        }
                    }
                    else
                    {
                        $arn
                    }
                }
            }
            else
            {
                Write-Warning "Stack does not exist: $StackName"
            }
        }
    }

    end
    {
        if ($Wait -and ($arns | Measure-Object).Count -gt 0)
        {
            Write-Host "Waiting for delete:`n$($arns -join "`n")"

            Wait-PSCFNStack -StackArn $arns -CredentialArguments $credentialArguments -StartTime $startTime
        }
        else
        {
            $arns
        }

        if ($deleteErrors)
        {
            throw "At least one stack failed to delete - see warnings above."
        }
    }
}
