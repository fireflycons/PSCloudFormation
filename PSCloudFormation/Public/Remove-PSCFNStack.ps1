function Remove-PSCFNStack
{
    <#
    .SYNOPSIS
        Delete one or more stacks.

    .DESCRIPTION
        Delete one or more stacks.

        Deletion of multiple stacks can be either sequential or parallel.
        If deleting a gruop of stacks where there are dependencies between them
        use the -Sequential switch and list the stacks in dependency order.

    .PARAMETER StackName
        Either stack names or the object returned by Get-CFNStack, New-CFNStack, Update-CFNStack
        and other functions in this module when run with -Wait.

    .PARAMETER Wait
        If set and -Sequential is not set (so deleting in parallel), wait for all stacks to be deleted before returning.

    .PARAMETER Sequential
        If set, delete stacks in the order they are specified on the command line or received from the pipeline,
        waiting for each stack to delete successfully before proceeding to the next one.

    .INPUTS
        System.String[]
            You can pipe the names or ARNs of the stacks to delete to this function

    .OUTPUTS
        None

    .EXAMPLE

        Remove-PSCFNStack -StackName MyStack

        Deletes a single stack.

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

        [switch]$Sequential
    )

    DynamicParam
    {
        #Create the RuntimeDefinedParameterDictionary
        New-Object System.Management.Automation.RuntimeDefinedParameterDictionary |
            New-CredentialDynamicParameters
    }

    begin
    {
        $endStates = @('DELETE_COMPLETE', 'DELETE_FAILED')
        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters
        $deleteBegin = [DateTime]::Now
    }

    process
    {
        $arns = $StackName |
            ForEach-Object {

            if (Test-StackExists -StackName $_ -CredentialArguments $credentialArguments)
            {
                $arn = (Get-CFNStack -StackName $_ @credentialArguments).StackId

                Remove-CFNStack -StackName $arn -Force

                if ($Sequential -or ($Wait -and ($StackName | Measure-Object).Count -eq 1))
                {
                    # Wait for this delete to complete before starting the next
                    Write-Host "Waiting for delete: $arn"
                    $stack = Wait-CFNStack -StackName $arn -Timeout ([TimeSpan]::FromMinutes(60).TotalSeconds) -Status $endStates @credentialArguments

                    if ($stack.StackStatus -like 'DELETE_FAILED')
                    {
                        Write-Host -ForegroundColor Red -BackgroundColor Black "Delete failed: $arn"
                        Write-Host -ForegroundColor Red -BackgroundColor Black (Get-StackFailureEvents -StackName $arn -CredentialArguments $credentialArguments | Sort-Object -Descending Timestamp | Out-String)

                        # Have to give up now as chained stack almost certainly is used by this one
                        throw $stack.StackStatusReason
                    }
                }
                else
                {
                    $arn
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

            while ($arns.Length -gt 0)
            {
                Start-Sleep -Seconds 5

                foreach ($arn in $arns)
                {
                    $stack = Get-CFNStack -StackName $arn @credentialArguments

                    if ($endStates -icontains $stack.StackStatus)
                    {
                        $arns = $arns | Where-Object { $_ -ine $arn }

                        if ($stack.StackStatus -like 'DELETE_FAILED')
                        {
                            Write-Host -ForegroundColor Red -BackgroundColor Black "Delete failed: $arn"
                            Write-Host -ForegroundColor Red -BackgroundColor Black "$($stack.StackStatusReason)"
                            Write-Host -ForegroundColor Red -BackgroundColor Black (
                                Get-StackFailureEvents -StackName $StackName -CredentialArguments $credentialArguments |
                                    Where-Object { $_.Timestamp -ge $deleteBegin } |
                                    Sort-Object -Descending Timestamp |
                                    Out-String
                            )
                        }
                        else
                        {
                            Write-Host "Delete complete: $arn"
                        }
                    }
                }
            }
        }
    }
}
