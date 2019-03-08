function New-PSCFNStack
{
    <#
    .SYNOPSIS
        Creates a stack.

    .DESCRIPTION
        Creates a stack.

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

        Template parameters with no default will become mandatory parameters to this function.
        If you do not supply them, you will be prompted for them and the help text for the
        parameter will be taken from the Description property of the parameter.

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

    .PARAMETER ClientRequestToken
        A unique identifier for this CreateStack request. Specify this token if you plan to retry requests so that AWS CloudFormation knows that you're not attempting to create a stack with the same name. You might retry CreateStack requests to ensure that AWS CloudFormation successfully received them.All events triggered by a given stack operation are assigned the same client request token, which you can use to track operations. For example, if you execute a CreateStack operation with the token token1, then all the StackEvents generated by that operation will have ClientRequestToken set as token1.In the console, stack operations display the client request token on the Events tab. Stack operations that are initiated from the console use the token format Console-StackOperation-ID, which helps you easily identify the stack operation . For example, if you create a stack using the console, each stack event would be assigned the same token in the following format: Console-CreateStack-7f59c3cf-00d2-40c7-b2ff-e75db0987002.

    .PARAMETER DisableRollback
        Set to true to disable rollback of the stack if stack creation failed. You can specify either DisableRollback or OnFailure, but not both.Default: false

    .PARAMETER EnableTerminationProtection
        Whether to enable termination protection on the specified stack. If a user attempts to delete a stack with termination protection enabled, the operation fails and the stack remains unchanged. For more information, see Protecting a Stack From Being Deleted in the AWS CloudFormation User Guide. Termination protection is disabled on stacks by default. For nested stacks, termination protection is set on the root stack and cannot be changed directly on the nested stack.

    .PARAMETER Force
        This parameter overrides confirmation prompts to force the cmdlet to continue its operation. This parameter should always be used with caution.

    .PARAMETER NotificationARNs
        The Simple Notification Service (SNS) topic ARNs to publish stack related events. You can find your SNS topic ARNs using the SNS console or your Command Line Interface (CLI).

    .PARAMETER OnFailure
        Determines what action will be taken if stack creation fails. This must be one of: DO_NOTHING, ROLLBACK, or DELETE. You can specify either OnFailure or DisableRollback, but not both.Default: ROLLBACK

    .PARAMETER ResourceType
        The template resource types that you have permissions to work with for this create stack action, such as AWS::EC2::Instance, AWS::EC2::*, or Custom::MyCustomInstance. Use the following syntax to describe template resource types: AWS::* (for all AWS resource), Custom::* (for all custom resources), Custom::logical_ID (for a specific custom resource), AWS::service_name::* (for all resources of a particular AWS service), and AWS::service_name::resource_logical_ID (for a specific AWS resource).If the list of resource types doesn't include a resource that you're creating, the stack creation fails. By default, AWS CloudFormation grants permissions to all resource types. AWS Identity and Access Management (IAM) uses this parameter for AWS CloudFormation-specific condition keys in IAM policies. For more information, see Controlling Access with AWS Identity and Access Management.

    .PARAMETER RoleARN
        The Amazon Resource Name (ARN) of an AWS Identity and Access Management (IAM) role that AWS CloudFormation assumes to create the stack. AWS CloudFormation uses the role's credentials to make calls on your behalf. AWS CloudFormation always uses this role for all future operations on the stack. As long as users have permission to operate on the stack, AWS CloudFormation uses this role even if the users don't have permission to pass it. Ensure that the role grants least privilege.If you don't specify a value, AWS CloudFormation uses the role that was previously associated with the stack. If no role is available, AWS CloudFormation uses a temporary session that is generated from your user credentials.

    .PARAMETER RollbackConfiguration_MonitoringTimeInMinute
        The amount of time, in minutes, during which CloudFormation should monitor all the rollback triggers after the stack creation or update operation deploys all necessary resources.The default is 0 minutes.If you specify a monitoring period but do not specify any rollback triggers, CloudFormation still waits the specified period of time before cleaning up old resources after update operations. You can use this monitoring period to perform any manual stack validation desired, and manually cancel the stack creation or update (using CancelUpdateStack, for example) as necessary.If you specify 0 for this parameter, CloudFormation still monitors the specified rollback triggers during stack creation and update operations. Then, for update operations, it begins disposing of old resources immediately once the operation completes.

    .PARAMETER RollbackConfiguration_RollbackTrigger
        The triggers to monitor during stack creation or update actions. By default, AWS CloudFormation saves the rollback triggers specified for a stack and applies them to any subsequent update operations for the stack, unless you specify otherwise. If you do specify rollback triggers for this parameter, those triggers replace any list of triggers previously specified for the stack.
        If a specified trigger is missing, the entire stack operation fails and is rolled back.

    .PARAMETER StackPolicyBody
        Structure containing the stack policy body. For more information, go to Prevent Updates to Stack Resources in the AWS CloudFormation User Guide. You can specify either the StackPolicyBody or the StackPolicyURL parameter, but not both.

    .PARAMETER StackPolicyURL
        Location of a file containing the stack policy. The URL must point to a policy (maximum size: 16 KB) located in an S3 bucket in the same region as the stack. You can specify either the StackPolicyBody or the StackPolicyURL parameter, but not both.

    .PARAMETER Tag
        Key-value pairs to associate with this stack. AWS CloudFormation also propagates these tags to the resources created in the stack. A maximum number of 50 tags can be specified.

    .PARAMETER TimeoutInMinutes
        The amount of time that can pass before the stack status becomes CREATE_FAILED; if DisableRollback is not set or is set to false, the stack will be rolled back.

    .PARAMETER Wait
        If set, wait for stack creation to complete before returning.

    .INPUTS
        System.String
            You can pipe the new stack name to this function

    .OUTPUTS
        System.String
            ARN of the new stack

    .NOTES
        This cmdlet genenerates additional dynamic command line parameters for all parameters found in the Parameters block of the supplied CloudFormation template

    .EXAMPLE

        New-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.0.0.0/16

        Creates a new stack from a local template file and waits for it to complete.
        This template would have 'VpcCidr' defined within its parameter block

    .EXAMPLE

        New-PSCFNStack -StackName MyStack -TemplateLocation https://s3-eu-west-1.amazonaws.com/mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.0.0.0/16

        As per the first example, but with the template located in S3.

    .EXAMPLE

        New-PSCFNStack -StackName MyStack -TemplateLocation s3://mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.0.0.0/16

        As per the first example, but using an S3 URL.
        Caveat to this mechanism is that you must have a default region set in the curent shell. The bucket is assumed to be in this region and the stack will also be built in this region.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string]$StackName,

        [Parameter(Mandatory = $true)]
        [string]$TemplateLocation,

        [ValidateSet('CAPABILITY_IAM', 'CAPABILITY_NAMED_IAM')]
        [Alias('Capability')]
        [string]$Capabilities,

        [string]$ClientRequestToken,

        [switch]$Force,

        [bool]$DisableRollback,

        [bool]$EnableTerminationProtection,

        [string[]]$NotificationARNs,

        [Amazon.CloudFormation.OnFailure]$OnFailure,

        [string[]]$ResourceType,

        [string]$RoleARN,

        [int]$RollbackConfiguration_MonitoringTimeInMinute,

        [Amazon.CloudFormation.Model.RollbackTrigger[]]$RollbackConfiguration_RollbackTrigger,

        [string]$StackPolicyBody,

        [string]$StackPolicyURL,

        [Alias('Tags')]
        [Amazon.CloudFormation.Model.Tag[]]$Tag,

        [int]$TimeoutInMinutes,

        [switch]$Wait
    )

    DynamicParam
    {
        #Create the RuntimeDefinedParameterDictionary
        New-Object System.Management.Automation.RuntimeDefinedParameterDictionary |
            New-CredentialDynamicParameters |
            New-TemplateDynamicParameters -TemplateLocation $TemplateLocation -EnforceMandatory
    }

    begin
    {
        $stackParameters = Get-CommandLineStackParameters -CallerBoundParameters $PSBoundParameters
        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters

        $passOnArguments = @{}
        $PSBoundParameters.Keys |
        Where-Object {
            @(
                'ClientRequestToken'
                'DisableRollback'
                'EnableTerminationProtection'
                'Force'
                'NotificationARNs'
                'OnFailure'
                'ResourceType'
                'RoleARN'
                'RollbackConfiguration_MonitoringTimeInMinute'
                'RollbackConfiguration_RollbackTrigger'
                'StackPolicyBody'
                'StackPolicyURL'
                'Tag'
                'TimeoutInMinutes'
            ) -icontains $_
        } |
        ForEach-Object {
            $passOnArguments.Add($_, $PSBoundParameters[$_])
        }

        $disableRollbackSet = ($PSBoundParameters.Keys -icontains 'DisableRollback' -and $DisableRollback)
    }

    end
    {
        try
        {
            if (Test-StackExists -StackName $StackName -CredentialArguments $credentialArguments)
            {
                throw "Stack already exists: $StackName"
            }

            $stackArgs = New-StackOperationArguments -StackName $StackName -TemplateLocation $TemplateLocation -Capabilities $Capabilities -StackParameters $stackParameters
            $arn = New-CFNStack @stackArgs @credentialArguments @passOnArguments

            if ($Wait)
            {
                Write-Host "Waiting for creation to complete"

                $stack = Wait-CFNStack -StackName $arn -Timeout ([TimeSpan]::FromMinutes(60).TotalSeconds) -Status @('CREATE_COMPLETE', 'ROLLBACK_IN_PROGRESS') @credentialArguments

                if ($stack.StackStatus -like '*ROLLBACK*')
                {
                    Write-Host -ForegroundColor Red -BackgroundColor Black "Create failed: $arn"
                    Write-Host -ForegroundColor Red -BackgroundColor Black (Get-StackFailureEvents -StackName $arn -CredentialArguments $credentialArguments | Sort-Object -Descending Timestamp | Out-String)

                    $updateFailedReason = $stack.StackStatusReason

                    if (-not $disableRollbackSet)
                    {
                        $updateStart = [DateTime]::Now

                        Write-Host "Waiting for rollback"

                        $stack = Wait-CFNStack -StackName $arn -Timeout ([TimeSpan]::FromMinutes(60).TotalSeconds) -Status @('ROLLBACK_COMPLETE', 'ROLLBACK_FAILED') @credentialArguments

                        if ($stack.StackStatus -like '*FAILED*')
                        {
                            Write-Host -ForegroundColor Red -BackgroundColor Black "Rollback failed: $arn"
                            Write-Host -ForegroundColor Red -BackgroundColor Black (
                                Get-StackFailureEvents -StackName $arn -CredentialArguments $credentialArguments |
                                    Where-Object { $_.Timestamp -ge $updateStart } |
                                    Sort-Object -Descending Timestamp |
                                    Out-String
                            )

                            $updateFailedReason += [Environment]::NewLine + $stack.StackStatusReason
                        }
                    }

                    throw $updateFailedReason
                }
            }

            # Emit ARN
            $arn
        }
        catch
        {
            Format-ExceptionDetail $_
            Write-Host
            throw
        }
    }
}
