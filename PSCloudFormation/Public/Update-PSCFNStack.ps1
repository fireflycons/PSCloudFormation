function Update-PSCFNStack
{
    <#
    .SYNOPSIS
        Updates a stack.

    .DESCRIPTION
        Updates a stack via creation and application of a changeset.

        DYNAMIC PARAMETERS

        Once the -TemplateLocation argument has been suppied on the command line
        the function reads the template and creates additional command line parameters
        for each of the entries found in the "Parameters" section of the template.
        These parameters are named as per each parameter in the template and defaults
        and validation rules created for them as defined by the template.

        Thus, if a template parameter has AllowedPattern and AllowedValues properties,
        the resultant function argument will permit TAB completion of the AllowedValues,
        assert that you have entered one of these, and for AllowedPattern, the function
        argument will assert the regular expression.

        Template parameters with no default that are not specified on the command line
        will be passed to the stack as Use Previous Value.

    .PARAMETER StackName
        Name for the new stack.

    .PARAMETER TemplateLocation
        Location of the template.
        This may be
        - Path to a local file
        - s3:// URL pointing to template in a bucket
        - https:// URL pointing to template in a bucket

    .PARAMETER Capabilities
        If the stack requires IAM capabilities, TAB auctocompletes between the capability types.

    .PARAMETER Wait
        If set, wait for stack update to complete before returning.

    .PARAMETER Force
        If set, do not ask for confirmation of the changeset before proceeding.

    .INPUTS
        System.String
            You can pipe the stack name or ARN to this function

    .OUTPUTS
        System.String
            ARN of the stack

    .EXAMPLE

        Update-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        Updates an existing stack of the same name or ARN from a local template file and waits for it to complete.
        This template would have 'VpcCidr' defined within its parameter block
        A changeset is created and displayed, and you are asked for confirmation befre proceeding.

    .EXAMPLE

        Update-PSCFNStack -StackName MyStack -TemplateLocation https://s3-eu-west-1.amazonaws.com/mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        As per the first example, but with the template located in S3.

    .EXAMPLE

        Update-PSCFNStack -StackName MyStack -TemplateLocation s3://mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        As per the first example, but using an S3 URL.
        Caveat to this mechanism is that you must have a default region set in the curent shell. The bucket is assumed to be in this region and the stack will also be built in this region.

    .EXAMPLE

        Update-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16 -Force

        As per the first example, but it begins the update without you being asked to confirm the change

    .NOTES
        This cmdlet genenerates additional dynamic command line parameters for all parameters found in the Parameters block of the supplied CloudFormation template
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [string]$StackName,

        [Parameter(Mandatory = $true)]
        [string]$TemplateLocation,

        [ValidateSet('CAPABILITY_IAM', 'CAPABILITY_NAMED_IAM')]
        [string]$Capabilities,

        [switch]$Wait,

        [switch]$Force
    )

    DynamicParam
    {
        #Create the RuntimeDefinedParameterDictionary
        New-Object System.Management.Automation.RuntimeDefinedParameterDictionary |
            New-CredentialDynamicParameters |
            New-TemplateDynamicParameters -TemplateLocation $TemplateLocation
    }

    begin
    {
        $stackParameters = Get-CommandLineStackParameters -CallerBoundParameters $PSBoundParameters
        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters
    }

    end
    {
        try
        {
            $stack = Get-CFNStack -StackName $StackName @credentialArguments
        }
        catch
        {
            throw "Stack $StackName does not exist"
        }

        # Add any parameters not present on command line
        # as Use Previous Value
        $stack.Parameters |
            ForEach-Object {

            if ($stackParameters.ParameterKey -inotcontains $_.ParameterKey)
            {
                $stackParameters += $(
                    $p = New-Object Amazon.CloudFormation.Model.Parameter
                    $p.ParameterKey = $_.ParameterKey
                    $p.UsePreviousValue = $true
                    $p
                )
            }
        }

        $changesetName = '{0}-{1}' -f [IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Module.Name), [int](([datetime]::UtcNow) - (get-date "1/1/1970")).TotalSeconds

        Write-Host "Creating change set $changesetName"

        $stackArgs = New-StackOperationArguments -StackName $StackName -TemplateLocation $TemplateLocation -Capabilities $Capabilities -StackParameters $stackParameters
        $csArn = New-CFNChangeSet -ChangeSetName $changesetName @stackArgs @credentialArguments
        $cs = Get-CFNChangeSet -ChangeSetName $csArn @credentialArguments

        while (('CREATE_COMPLETE', 'FAILED') -inotcontains $cs.Status)
        {
            Start-Sleep -Seconds 1
            $cs = Get-CFNChangeSet -ChangeSetName $csArn @credentialArguments
        }

        if ($cs.Status -ieq 'FAILED')
        {
            Write-Host -ForegroundColor Red -BackgroundColor Black "Changeset $changesetName failed to create: $($cs.StatusReason)"
            throw "Changeset failed to create"
        }

        Write-Host ($cs.Changes.ResourceChange | Select-Object Action, LogicalResourceId, PhysicalResourceId, ResourceType | Format-Table | Out-String)

        if (-not $Force)
        {
            $choice = $host.ui.PromptForChoice(
                'Begin the stack update now?',
                $null,
                @(
                    New-Object System.Management.Automation.Host.ChoiceDescription ('&Yes', "Start rebuild now." )
                    New-Object System.Management.Automation.Host.ChoiceDescription ('&No', 'Abort operation.')
                ),
                0
            )

            if ($choice -ne 0)
            {
                throw "Aborted."
            }
        }

        Write-Host "Updating stack $StackName"
        $updateStart = [DateTime]::Now

        $arn = (Get-CFNStack -StackName $StackName @credentialArguments).StackId
        Start-CFNChangeSet -StackName $StackName -ChangeSetName $changesetName @credentialArguments

        if ($Wait)
        {
            Write-Host "Waiting for update to complete"

            $stack = Wait-CFNStack -StackName $arn -Timeout ([TimeSpan]::FromMinutes(60).TotalSeconds) -Status @('UPDATE_COMPLETE', 'UPDATE_ROLLBACK_IN_PROGRESS') @credentialArguments

            if ($stack.StackStatus -like '*ROLLBACK*')
            {
                Write-Host -ForegroundColor Red -BackgroundColor Black "Update failed: $arn"
                Write-Host -ForegroundColor Red -BackgroundColor Black (
                    Get-StackFailureEvents -StackName $arn -CredentialArguments $credentialArguments |
                        Where-Object { $_.Timestamp -ge $updateStart } |
                        Sort-Object -Descending Timestamp |
                        Out-String
                )

                throw $stack.StackStatusReason
            }

            # Emit ARN
            $arn
        }
        else
        {
            # Emit ARN
            $arn
        }
    }
}
