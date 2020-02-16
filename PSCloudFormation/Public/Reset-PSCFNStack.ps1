function Reset-PSCFNStack
{
    <#
    .SYNOPSIS
        Delete and recreate an existing stack

    .DESCRIPTION
        Completely replace an existing stack
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

        Template parameters with no default will become mandatory parameters to this function.
        If you do not supply them, you will be prompted for them and the help text for the
        parameter will be taken from the Description property of the parameter.

    .PARAMETER StackName
        Name of the stack to replace.

    .PARAMETER Tag
        Key-value pairs to associate with this stack. AWS CloudFormation also propagates these tags to the resources created in the stack. A maximum number of 50 tags can be specified.

    .PARAMETER TemplateLocation
        Location of the template.
        This may be
        - Path to a local file
        - s3:// URL pointing to template in a bucket
        - https:// URL pointing to template in a bucket
        This cmdlet can accept pipeline input from New-PSCFNPackage, however there are caveats! See notes section.

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

    .PARAMETER ParameterFile
        If present, path to a JSON file containing a list of parameter structures as defined for 'aws cloudformation create-stack'. If a parameter of the same name is defined on the command line, the command line takes precedence.
        If your stack has a parameter with the same name as one of the parameters to this cmdlet, then you *must* set the stack parameter via a parameter file.

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
        [System.String], [PSCloudFomation.Packager.Package]
            You can pipe the CloudFormation Template to this command (see Notes).

    .OUTPUTS
        [System.String] - ARN of the new stack
        [Amazon.CloudFormation.StackStatus] - Status of last operation.

    .NOTES
        This cmdlet genenerates additional dynamic command line parameters for all parameters found in the Parameters block of the supplied CloudFormation template,
        except for when the template is supplied via the pipeline e.g. from New-PSCFNPackage. Due to the complexities of pipleine processing, it is not possible to
        determine the template details when composing the command. If you need to pass new values for parameters to the stack, then use a parameter file.

        See also https://github.com/fireflycons/PSCloudFormation/blob/master/static/resource-import.md

    .EXAMPLE

        Reset-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        First deletes any existing stack of the same name or ARN, then creates a new stack from a local template file and waits for it to complete.
        This template would have 'VpcCidr' defined within its parameter block

    .EXAMPLE

        Reset-PSCFNStack -StackName MyStack -TemplateLocation https://s3-eu-west-1.amazonaws.com/mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        As per the first example, but with the template located in S3.

    .EXAMPLE

        Reset-PSCFNStack -StackName MyStack -TemplateLocation s3://mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        As per the first example, but using an S3 URL.
        Caveat to this mechanism is that you must have a default region set in the curent shell. The bucket is assumed to be in this region and the stack will also be built in this region.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$StackName,

        [Parameter(ValueFromPipelineByPropertyName = $true, Mandatory)]
        [string[]]$TemplateLocation,

        [ValidateSet('CAPABILITY_IAM', 'CAPABILITY_NAMED_IAM', 'CAPABILITY_AUTO_EXPAND')]
        [string[]]$Capabilities,

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

        [string]$ParameterFile,

        [switch]$Wait
    )

    DynamicParam
    {
        # Create the RuntimeDefinedParameterDictionary, storing in a variable for use later on
        $runtimeDefinedParameterDictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary | New-CredentialDynamicParameters

        $preparedStackParameters = $false

        if ($TemplateLocation)
        {
            # If we know the template location, we can build dynamic parameters for it
            # If the template location is piped in from e.g. New-PSCFNPackage, then we don't know it yet
            $runtimeDefinedParameterDictionary = $runtimeDefinedParameterDictionary | New-TemplateDynamicParameters -StackName $StackName -TemplateLocation ($TemplateLocation | Select-Object -First 1)
            $preparedStackParameters = $true
        }

        # Emit dynamic parameters
        $runtimeDefinedParameterDictionary
    }

    begin
    {
        $stackParameters = Get-CommandLineStackParameters -CallerBoundParameters $PSBoundParameters
        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters
        $processIterations = 0
    }

    process
    {
        if (++$processIterations -gt 1)
        {
            # Kludgy, but necessary as we have to have a process block to pipe in the template location
            throw "Cannot process more than one template for a stack"
        }

        if (-not $preparedStackParameters)
        {
            $runtimeDefinedParameterDictionary = $runtimeDefinedParameterDictionary | New-TemplateDynamicParameters -StackName $StackName -TemplateLocation ($TemplateLocation | Select-Object -First 1)
        }
    }

    end
    {
        # Pass all Update-PSCFNStack parameters except 'Rebuild' to New-PSCFNStack
        # This will include any common credential and template parameters
        $createParameters = @{}
        $PSBoundParameters.Keys |
            Where-Object { $_ -ine 'Rebuild'} |
            ForEach-Object {

            $value = $(
                if ($_ -eq 'TemplateLocation')
                {
                    $PSBoundParameters[$_] | Select-Object -First 1
                }
                else
                {
                    $PSBoundParameters[$_]
                }
            )

            $createParameters.Add($_, $value)
        }

        try
        {
            Remove-PSCFNStack -StackName $StackName -Wait @credentialArguments -ThrowOnAbort -Force:$Force
        }
        catch
        {
            Write-Warning $_.Exception.Message
            return
        }

        New-PSCFNStack @createParameters
    }
}
