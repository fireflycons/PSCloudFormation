function Update-PSCFNStack
{
    <#
    .SYNOPSIS
        Updates a stack.

    .DESCRIPTION
        Updates a stack via creation and application of a changeset.
        If -Wait is specified, stack events are output to the console including events from any nested stacks.

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
        Name of the stack to update.

    .PARAMETER TemplateLocation
        Location of the template.
        This may be
        - Path to a local file
        - s3:// URL pointing to template in a bucket
        - https:// URL pointing to template in a bucket
        Conditional: You must specify only TemplateLocationL, or set the UsePreviousTemplate to true.

    .PARAMETER Capabilities
        If the stack requires IAM capabilities, TAB auctocompletes between the capability types.

    .PARAMETER NotificationARNs
        The Simple Notification Service (SNS) topic ARNs to publish stack related events. You can find your SNS topic ARNs using the SNS console or your Command Line Interface (CLI).

    .PARAMETER ParameterFile
        If present, path to a JSON file containing a list of parameter structures as defined for 'aws cloudformation create-stack'. If a parameter of the same name is defined on the command line, the command line takes precedence.
        If your stack has a parameter with the same name as one of the parameters to this cmdlet, then you *must* set the stack parameter via a parameter file.

    .PARAMETER ResourceType
        The template resource types that you have permissions to work with for this create stack action, such as AWS::EC2::Instance, AWS::EC2::*, or Custom::MyCustomInstance. Use the following syntax to describe template resource types: AWS::* (for all AWS resource), Custom::* (for all custom resources), Custom::logical_ID (for a specific custom resource), AWS::service_name::* (for all resources of a particular AWS service), and AWS::service_name::resource_logical_ID (for a specific AWS resource).If the list of resource types doesn't include a resource that you're creating, the stack creation fails. By default, AWS CloudFormation grants permissions to all resource types. AWS Identity and Access Management (IAM) uses this parameter for AWS CloudFormation-specific condition keys in IAM policies. For more information, see Controlling Access with AWS Identity and Access Management.

    .PARAMETER RollBackConfiguration
        The rollback triggers for AWS CloudFormation to monitor during stack creation and updating operations, and for the specified monitoring period afterwards.

    .PARAMETER RoleARN
        The Amazon Resource Name (ARN) of an AWS Identity and Access Management (IAM) role that AWS CloudFormation assumes to create the stack. AWS CloudFormation uses the role's credentials to make calls on your behalf. AWS CloudFormation always uses this role for all future operations on the stack. As long as users have permission to operate on the stack, AWS CloudFormation uses this role even if the users don't have permission to pass it. Ensure that the role grants least privilege.If you don't specify a value, AWS CloudFormation uses the role that was previously associated with the stack. If no role is available, AWS CloudFormation uses a temporary session that is generated from your user credentials.

    .PARAMETER Tag
        Key-value pairs to associate with this stack. AWS CloudFormation also propagates these tags to the resources created in the stack. A maximum number of 50 tags can be specified.

    .PARAMETER UsePreviousTemplate
        Reuse the existing template that is associated with the stack that you are updating. Conditional: You must specify only TemplateLocationL, or set the UsePreviousTemplate to true.

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

        [string]$TemplateLocation,

        [ValidateSet('CAPABILITY_IAM', 'CAPABILITY_NAMED_IAM')]
        [string]$Capabilities,

        [string[]]$NotificationARNs,

        [string[]]$ResourceType,

        [string]$RoleARN,

        [Amazon.CloudFormation.Model.RollbackConfiguration]$RollbackConfiguration,

        [Alias('Tags')]
        [Amazon.CloudFormation.Model.Tag[]]$Tag,

        [switch]$UsePreviousTemplate,

        [string]$ParameterFile,

        [switch]$Wait,

        [switch]$Force
    )

    DynamicParam
    {
        $templateArguments = @{}
        $PSBoundParameters.GetEnumerator() |
            Where-Object {
            ('TemplateLocation', 'UsePreviousTemplate', 'StackName') -icontains $_.Key
        } |
            ForEach-Object {

                if ($_.Value -is [System.Management.Automation.SwitchParameter])
                {
                    $templateArguments.Add($_.Key, $_.Value.ToBool())
                }
                else
                {
                    $templateArguments.Add($_.Key, $_.Value)
                }
        }

        #Create the RuntimeDefinedParameterDictionary
        New-Object System.Management.Automation.RuntimeDefinedParameterDictionary |
            New-CredentialDynamicParameters |
            New-TemplateDynamicParameters @templateArguments
    }

    begin
    {
        $stackParameters = Get-CommandLineStackParameters -CallerBoundParameters $PSBoundParameters
        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters
        $changeSetPassOnArguments = @{}
        $PSBoundParameters.Keys |
            Where-Object {
            @(
                'Force'
                'NotificationARNs'
                'ResourceType'
                'RoleARN'
                'RollbackConfiguration'
                'Tag'
            ) -icontains $_
        } |
            ForEach-Object {
            $changeSetPassOnArguments.Add($_, $PSBoundParameters[$_])
        }

    }

    end
    {
        try
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

            $stackArgs = New-StackOperationArguments -StackName $StackName -TemplateLocation $TemplateLocation -Capabilities $Capabilities -StackParameters $stackParameters -CredentialArguments $credentialArguments -UsePreviousTemplate ([bool]$UsePreviousTemplate)
            $csArn = New-CFNChangeSet -ChangeSetName $changesetName @stackArgs @credentialArguments @changeSetPassOnArguments
            $cs = Get-CFNChangeSet -ChangeSetName $csArn @credentialArguments

            while (('CREATE_COMPLETE', 'FAILED') -inotcontains $cs.Status)
            {
                Start-Sleep -Seconds 1
                $cs = Get-CFNChangeSet -ChangeSetName $csArn @credentialArguments
            }

            if ($cs.Status -ieq 'FAILED')
            {
                if ($cs.StatusReason -ilike "*The submitted information didn't contain changes*")
                {
                    Write-Warning "Changeset $changesetName failed to create: $($cs.StatusReason)"
                    return $stack.StackId
                }

                Write-Host -ForegroundColor Red -BackgroundColor Black "Changeset $changesetName failed to create: $($cs.StatusReason)"
                throw "Changeset failed to create"
            }

            Write-Host ($cs.Changes.ResourceChange | Format-Table -Property Action, LogicalResourceId, ResourceType, Replacement, PhysicalResourceId | Out-String)

            if (-not $Force)
            {
                $choice = $host.ui.PromptForChoice(
                    'Begin the stack update now?',
                    $null,
                    @(
                        New-Object System.Management.Automation.Host.ChoiceDescription ('&Yes', "Start rebuild now." )
                        New-Object System.Management.Automation.Host.ChoiceDescription ('&No', 'Cancel operation.')
                    ),
                    0
                )

                if ($choice -ne 0)
                {
                    Write-Warning "Update cancelled."
                    return $stack.StackId
                }
            }

            Write-Host "Updating stack $StackName"

            $arn = (Get-CFNStack -StackName $StackName @credentialArguments).StackId

            # Issue #14 - Get-CFNStackEvents returns timestamp in local time
            $startTime = [DateTime]::Now

            Start-CFNChangeSet -StackName $StackName -ChangeSetName $changesetName @credentialArguments

            if ($Wait)
            {
                Write-Host "Waiting for update to complete"

                if (-not (Wait-PSCFNStack -StackArn $arn -CredentialArguments $credentialArguments -StartTime $startTime))
                {
                    throw "Update unsuccessful"
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
        catch
        {
            Format-ExceptionDetail $_
            Write-Host
            throw
        }
    }
}
