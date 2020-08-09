---
external help file: Firefly.PSCloudFormation.dll-Help.xml
Module Name: PSCloudFormation
online version: (https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/resource-import.html)
schema: 2.0.0
---

# Update-PSCFNStack

## SYNOPSIS
Calls the AWS CloudFormation UpdateStack API operation.

## SYNTAX

```
Update-PSCFNStack [-StackPolicyDuringUpdateLocation <String>] [-UsePreviousTemplate]
 [-ResourcesToImport <String>] [-Wait] [-Capabilities <String[]>] [-ForceS3] [-NotificationARNs <String[]>]
 [-ParameterFile <String>] [-ResourceType <String[]>] [-RollbackConfiguration_MonitoringTimeInMinute <Int32>]
 [-RollbackConfiguration_RollbackTrigger <RollbackTrigger[]>] [-StackPolicyLocation <String>] [-Tag <Tag[]>]
 [-TemplateLocation <String>] [-ClientRequestToken <String>] [-Force] [-PassThru] [-RoleARN <String>]
 [-StackName] <String> [-AccessKey <String>] [-Credential <AWSCredentials>] [-EndpointUrl <String>]
 [-NetworkCredential <PSCredential>] [-ProfileLocation <String>] [-ProfileName <String>] [-Region <Object>]
 [-S3EndpointUrl <String>] [-SecretKey <String>] [-SessionToken <String>] [-STSEndpointUrl <String>]
 [<CommonParameters>]
```

## DESCRIPTION
A change set is first created and displayed to the user.
Unless -Force is specified, the user may choose to continue or abandon at this stage.
The call does not return until the stack update has completed unless -PassThru is present, in which case it returns immediately and you can check the status of the stack via the DescribeStacks API Stack events for this template and any nested stacks are output to the console.

If -Wait is present, and a stack is found to be updating as a result of another process, this command will wait for that operation to complete following the stack events, prior to submitting the change set request.

## EXAMPLES

### EXAMPLE 1
```
Update-PSCFNStack -StackName "my-stack" -TemplateBody "{TEMPLATE CONTENT HERE}" -PK1 PV1 -PK2 PV2
```

Updates the stack my-stack and follows the output until the operation completes.
The template is parsed from the supplied content with customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
If update of the stack fails, it will be rolled back.

### EXAMPLE 2
```
Update-PSCFNStack -StackName "my-stack" -UsePreviousTemplate -PK1 PV1 -PK2 PV2
```

Updates the stack my-stack and follows the output until the operation completes.
The template currently associated with the stack is used and updated with new customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
If update of the stack fails, it will be rolled back.

### EXAMPLE 3
```
Update-PSCFNStack -StackName "my-stack" -TemplateLocation ~/my-templates/template.json -ResourcesToImport ~/my-templates/import-resources.yaml
```

Performs a resource import on the stack my-stack and follows the output until the operation completes.
The template is parsed from the supplied content and resources to import are parsed from the supplied import file.
Note that when importing resources, only IMPORT changes are permitted.
Nothing else in the stack can be changed in the same operation.
If update of the stack fails, it will be rolled back.

## PARAMETERS

### -StackPolicyDuringUpdateLocation
Structure containing the temporary overriding stack policy body.
For more information, go to Prevent Updates to Stack Resources in the AWS CloudFormation User Guide.
If you want to update protected resources, specify a temporary overriding stack policy during this update.
If you do not specify a stack policy, the current policy that is associated with the stack will be used.
You can specify either a string, path to a file, or URL of a object in S3 that contains the policy body.

```yaml
Type: String
Parameter Sets: (All)
Aliases: StackPolicyDuringUpdateBody, StackPolicyDuringUpdateURL

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -UsePreviousTemplate
Reuse the existing template that is associated with the stack that you are updating.
Conditional: You must specify only one of the following parameters: TemplateLocation or set the UsePreviousTemplate to true.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ResourcesToImport
The resources to import into your stack.

If you created an AWS resource outside of AWS CloudFormation management, you can bring this existing resource into AWS CloudFormation management using resource import.
You can manage your resources using AWS CloudFormation regardless of where they were created without having to delete and re-create them as part of a stack.
Note that when performing an import, this is the only change that can happen to the stack.
If any other resources are changed, the changeset will fail to create.
You can specify either a string, path to a file, or URL of a object in S3 that contains the resource import body as JSON or YAML.

You can specify either a string, path to a file, or URL of a object in S3 that contains the resource import body as JSON or YAML.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Wait
If set, and the target stack is found to have an operation already in progress, then the command waits until that operation completes, printing out stack events as it goes.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Capabilities
In some cases, you must explicitly acknowledge that your stack template contains certain capabilities in order for AWS CloudFormation to create the stack.
CAPABILITY_IAM and CAPABILITY_NAMED_IAM Some stack templates might include resources that can affect permissions in your AWS account; for example, by creating new AWS Identity and Access Management (IAM) users.
For those stacks, you must explicitly acknowledge this by specifying one of these capabilities.
CAPABILITY_AUTO_EXPAND Some template contain macros.
Macros perform custom processing on templates; this can include simple actions like find-and-replace operations, all the way to extensive transformations of entire templates.
Because of this, users typically create a change set from the processed template, so that they can review the changes resulting from the macros before actually creating the stack.
If your stack template contains one or more macros, and you choose to create a stack directly from the processed template, without first reviewing the resulting changes in a change set, you must acknowledge this capability.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Capability

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ForceS3
If present, forces upload of a local template (file or string body) to S3, irrespective of whether the template size is over the maximum of 51,200 bytes

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -NotificationARNs
The Simple Notification Service (SNS) topic ARNs to publish stack related events.
You can find your SNS topic ARNs using the SNS console or your Command Line Interface (CLI).

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ParameterFile
If present, location of a list of stack parameters to apply.
This is a JSON or YAML list of parameter structures with fields ParameterKey and ParameterValue.
This is similar to aws cloudformation create-stack except the other fields defined for that are ignored here.
Parameters not supplied to an update operation are assumed to be UsePreviousValue.
If a parameter of the same name is defined on the command line, the command line takes precedence.
If your stack has a parameter with the same name as one of the parameters to this cmdlet, then you *must* set the stack parameter via a parameter file.

You can specify either a string containing JSON or YAML, or path to a file that contains the parameters.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ResourceType
The template resource types that you have permissions to work with for this create stack action, such as AWS::EC2::Instance, AWS::EC2::*, or Custom::MyCustomInstance.
Use the following syntax to describe template resource types: AWS::* (for all AWS resource), Custom::* (for all custom resources), Custom::logical_ID (for a specific custom resource), AWS::service_name::* (for all resources of a particular AWS service), and AWS::service_name::resource_logical_ID (for a specific AWS resource).
If the list of resource types doesn't include a resource that you're creating, the stack creation fails.
By default, AWS CloudFormation grants permissions to all resource types.
AWS Identity and Access Management (IAM) uses this parameter for AWS CloudFormation-specific condition keys in IAM policies.
For more information, see Controlling Access with AWS Identity and Access Management.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RollbackConfiguration_MonitoringTimeInMinute
The amount of time, in minutes, during which CloudFormation should monitor all the rollback triggers after the stack creation or update operation deploys all necessary resources.
The default is 0 minutes.If you specify a monitoring period but do not specify any rollback triggers, CloudFormation still waits the specified period of time before cleaning up old resources after update operations.
You can use this monitoring period to perform any manual stack validation desired, and manually cancel the stack creation or update (using CancelUpdateStack, for example) as necessary.
If you specify 0 for this parameter, CloudFormation still monitors the specified rollback triggers during stack creation and update operations.
Then, for update operations, it begins disposing of old resources immediately once the operation completes.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases: RollbackConfiguration_MonitoringTimeInMinutes

Required: False
Position: Named
Default value: 0
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RollbackConfiguration_RollbackTrigger
The triggers to monitor during stack creation or update actions.
By default, AWS CloudFormation saves the rollback triggers specified for a stack and applies them to any subsequent update operations for the stack, unless you specify otherwise.
If you do specify rollback triggers for this parameter, those triggers replace any list of triggers previously specified for the stack.
If a specified trigger is missing, the entire stack operation fails and is rolled back.

```yaml
Type: RollbackTrigger[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StackPolicyLocation
Structure containing the stack policy body.
For more information, go to Prevent Updates to Stack Resources in the AWS CloudFormation User Guide.
You can specify either a string, path to a file, or URL of a object in S3 that contains the policy body.

```yaml
Type: String
Parameter Sets: (All)
Aliases: StackPolicyBody, StackPolicyURL

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Tag
Key-value pairs to associate with this stack.
AWS CloudFormation also propagates these tags to the resources created in the stack.
A maximum number of 50 tags can be specified.

```yaml
Type: Tag[]
Parameter Sets: (All)
Aliases: Tags

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -TemplateLocation
Structure containing the template body.
For more information, go to Template Anatomy in the AWS CloudFormation User Guide.

You can pipe a template body to this command, e.g.
from the output of the New-PSCFNPackage command.

You can specify either a string, path to a file, or URL of a object in S3 that contains the template body.

```yaml
Type: String
Parameter Sets: (All)
Aliases: TemplateBody, TemplateURL

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -ClientRequestToken
A unique identifier for this CreateStack request.
Specify this token if you plan to retry requests so that AWS CloudFormation knows that you're not attempting to create a stack with the same name.
You might retry CreateStack requests to ensure that AWS CloudFormation successfully received them.
All events triggered by a given stack operation are assigned the same client request token, which you can use to track operations.
For example, if you execute a CreateStack operation with the token token1, then all the StackEvents generated by that operation will have ClientRequestToken set as token1.
In the console, stack operations display the client request token on the Events tab.
Stack operations that are initiated from the console use the token format Console-StackOperation-ID, which helps you easily identify the stack operation .
For example, if you create a stack using the console, each stack event would be assigned the same token in the following format: Console-CreateStack-7f59c3cf-00d2-40c7-b2ff-e75db0987002.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Force
This parameter overrides confirmation prompts to force the cmdlet to continue its operation.
This parameter should always be used with caution.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -PassThru
If this is set, then the operation returns immediately after submitting the request to CloudFormation.
If not set, then the operation is followed to completion, with stack events being output to the console.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RoleARN
The Amazon Resource Name (ARN) of an AWS Identity and Access Management (IAM) role that AWS CloudFormation assumes to create the stack.
AWS CloudFormation uses the role's credentials to make calls on your behalf.
AWS CloudFormation always uses this role for all future operations on the stack.
As long as users have permission to operate on the stack, AWS CloudFormation uses this role even if the users don't have permission to pass it.
Ensure that the role grants least privilege.If you don't specify a value, AWS CloudFormation uses the role that was previously associated with the stack.
If no role is available, AWS CloudFormation uses a temporary session that is generated from your user credentials.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StackName
The name that is associated with the stack.
The name must be unique in the Region in which you are creating the stack.A stack name can contain only alphanumeric characters (case sensitive) and hyphens.
It must start with an alphabetic character and cannot be longer than 128 characters.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AccessKey
The AWS access key for the user account.
This can be a temporary access key if the corresponding session token is supplied to the -SessionToken parameter.

```yaml
Type: String
Parameter Sets: (All)
Aliases: AK

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Credential
An AWSCredentials object instance containing access and secret key information, and optionally a token for session-based credentials.

```yaml
Type: AWSCredentials
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -EndpointUrl
The endpoint to make CloudFormation calls against.

The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
Only specify this parameter if you must direct the call to a specific custom endpoint, e.g.
if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -NetworkCredential
Used with SAML-based authentication when ProfileName references a SAML role profile.
Contains the network credentials to be supplied during authentication with the configured identity provider's endpoint.
This parameter is not required if the user's default network identity can or should be used during authentication.

```yaml
Type: PSCredential
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -ProfileLocation
Used to specify the name and location of the ini-format credential file (shared with the AWS CLI and other AWS SDKs)

If this optional parameter is omitted this cmdlet will search the encrypted credential file used by the AWS SDK for .NET and AWS Toolkit for Visual Studio first.
If the profile is not found then the cmdlet will search in the ini-format credential file at the default location: (user's home directory)\.aws\credentials.

If this parameter is specified then this cmdlet will only search the ini-format credential file at the location given.

As the current folder can vary in a shell or during script execution it is advised that you use specify a fully qualified path instead of a relative path.

```yaml
Type: String
Parameter Sets: (All)
Aliases: AWSProfilesLocation, ProfilesLocation

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProfileName
The user-defined name of an AWS credentials or SAML-based role profile containing credential information.
The profile is expected to be found in the secure credential file shared with the AWS SDK for .NET and AWS Toolkit for Visual Studio.
You can also specify the name of a profile stored in the .ini-format credential file used with the AWS CLI and other AWS SDKs.

```yaml
Type: String
Parameter Sets: (All)
Aliases: StoredCredentials, AWSProfileName

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Region
The system name of an AWS region or an AWSRegion instance.
This governs the endpoint that will be used when calling service operations.
Note that the AWS resources referenced in a call are usually region-specific.

```yaml
Type: Object
Parameter Sets: (All)
Aliases: RegionToCall

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -S3EndpointUrl
The endpoint to make S3 calls against.

S3 is used by these cmdlets for managing S3 based templates and by the packager for uploading code artifacts and nested templates.

The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
Only specify this parameter if you must direct the call to a specific custom endpoint, e.g.
if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SecretKey
The AWS secret key for the user account.
This can be a temporary secret key if the corresponding session token is supplied to the -SessionToken parameter.

```yaml
Type: String
Parameter Sets: (All)
Aliases: SK, SecretAccessKey

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SessionToken
The session token if the access and secret keys are temporary session-based credentials.

```yaml
Type: String
Parameter Sets: (All)
Aliases: ST

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -STSEndpointUrl
The endpoint to make STS calls against.

STS is used only if creating a bucket to store oversize templates and packager artifacts to get the caller account ID to use as part of the generated bucket name.

The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
Only specify this parameter if you must direct the call to a specific custom endpoint, e.g.
if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
Structure containing the temporary overriding stack policy body.
For more information, go to Prevent Updates to Stack Resources in the AWS CloudFormation User Guide.
If you want to update protected resources, specify a temporary overriding stack policy during this update.
If you do not specify a stack policy, the current policy that is associated with the stack will be used.
You can specify either a string, path to a file, or URL of a object in S3 that contains the policy body.

### System.Management.Automation.SwitchParameter
Reuse the existing template that is associated with the stack that you are updating.
Conditional: You must specify only one of the following parameters: TemplateLocation or set the UsePreviousTemplate to true.

### System.String
The resources to import into your stack.

If you created an AWS resource outside of AWS CloudFormation management, you can bring this existing resource into AWS CloudFormation management using resource import.
You can manage your resources using AWS CloudFormation regardless of where they were created without having to delete and re-create them as part of a stack.
Note that when performing an import, this is the only change that can happen to the stack.
If any other resources are changed, the changeset will fail to create.
You can specify either a string, path to a file, or URL of a object in S3 that contains the resource import body as JSON or YAML.

You can specify either a string, path to a file, or URL of a object in S3 that contains the resource import body as JSON or YAML.

### System.Management.Automation.SwitchParameter
If set, and the target stack is found to have an operation already in progress, then the command waits until that operation completes, printing out stack events as it goes.

### System.String[]
In some cases, you must explicitly acknowledge that your stack template contains certain capabilities in order for AWS CloudFormation to create the stack.
CAPABILITY_IAM and CAPABILITY_NAMED_IAM Some stack templates might include resources that can affect permissions in your AWS account; for example, by creating new AWS Identity and Access Management (IAM) users.
For those stacks, you must explicitly acknowledge this by specifying one of these capabilities.
CAPABILITY_AUTO_EXPAND Some template contain macros.
Macros perform custom processing on templates; this can include simple actions like find-and-replace operations, all the way to extensive transformations of entire templates.
Because of this, users typically create a change set from the processed template, so that they can review the changes resulting from the macros before actually creating the stack.
If your stack template contains one or more macros, and you choose to create a stack directly from the processed template, without first reviewing the resulting changes in a change set, you must acknowledge this capability.

### System.String[]
The Simple Notification Service (SNS) topic ARNs to publish stack related events.
You can find your SNS topic ARNs using the SNS console or your Command Line Interface (CLI).

### System.String
If present, location of a list of stack parameters to apply.
This is a JSON or YAML list of parameter structures with fields ParameterKey and ParameterValue.
This is similar to aws cloudformation create-stack except the other fields defined for that are ignored here.
Parameters not supplied to an update operation are assumed to be UsePreviousValue.
If a parameter of the same name is defined on the command line, the command line takes precedence.
If your stack has a parameter with the same name as one of the parameters to this cmdlet, then you *must* set the stack parameter via a parameter file.

You can specify either a string containing JSON or YAML, or path to a file that contains the parameters.

### System.String[]
The template resource types that you have permissions to work with for this create stack action, such as AWS::EC2::Instance, AWS::EC2::*, or Custom::MyCustomInstance.
Use the following syntax to describe template resource types: AWS::* (for all AWS resource), Custom::* (for all custom resources), Custom::logical_ID (for a specific custom resource), AWS::service_name::* (for all resources of a particular AWS service), and AWS::service_name::resource_logical_ID (for a specific AWS resource).
If the list of resource types doesn't include a resource that you're creating, the stack creation fails.
By default, AWS CloudFormation grants permissions to all resource types.
AWS Identity and Access Management (IAM) uses this parameter for AWS CloudFormation-specific condition keys in IAM policies.
For more information, see Controlling Access with AWS Identity and Access Management.

### System.Int32
The amount of time, in minutes, during which CloudFormation should monitor all the rollback triggers after the stack creation or update operation deploys all necessary resources.
The default is 0 minutes.If you specify a monitoring period but do not specify any rollback triggers, CloudFormation still waits the specified period of time before cleaning up old resources after update operations.
You can use this monitoring period to perform any manual stack validation desired, and manually cancel the stack creation or update (using CancelUpdateStack, for example) as necessary.
If you specify 0 for this parameter, CloudFormation still monitors the specified rollback triggers during stack creation and update operations.
Then, for update operations, it begins disposing of old resources immediately once the operation completes.

### Amazon.CloudFormation.Model.RollbackTrigger[]
The triggers to monitor during stack creation or update actions.
By default, AWS CloudFormation saves the rollback triggers specified for a stack and applies them to any subsequent update operations for the stack, unless you specify otherwise.
If you do specify rollback triggers for this parameter, those triggers replace any list of triggers previously specified for the stack.
If a specified trigger is missing, the entire stack operation fails and is rolled back.

### System.String
Structure containing the stack policy body.
For more information, go to Prevent Updates to Stack Resources in the AWS CloudFormation User Guide.
You can specify either a string, path to a file, or URL of a object in S3 that contains the policy body.

### Amazon.CloudFormation.Model.Tag[]
Key-value pairs to associate with this stack.
AWS CloudFormation also propagates these tags to the resources created in the stack.
A maximum number of 50 tags can be specified.

### System.String
Structure containing the template body.
For more information, go to Template Anatomy in the AWS CloudFormation User Guide.

You can pipe a template body to this command, e.g.
from the output of the New-PSCFNPackage command.

You can specify either a string, path to a file, or URL of a object in S3 that contains the template body.

### System.String
A unique identifier for this CreateStack request.
Specify this token if you plan to retry requests so that AWS CloudFormation knows that you're not attempting to create a stack with the same name.
You might retry CreateStack requests to ensure that AWS CloudFormation successfully received them.
All events triggered by a given stack operation are assigned the same client request token, which you can use to track operations.
For example, if you execute a CreateStack operation with the token token1, then all the StackEvents generated by that operation will have ClientRequestToken set as token1.
In the console, stack operations display the client request token on the Events tab.
Stack operations that are initiated from the console use the token format Console-StackOperation-ID, which helps you easily identify the stack operation .
For example, if you create a stack using the console, each stack event would be assigned the same token in the following format: Console-CreateStack-7f59c3cf-00d2-40c7-b2ff-e75db0987002.

### System.Management.Automation.SwitchParameter
This parameter overrides confirmation prompts to force the cmdlet to continue its operation.
This parameter should always be used with caution.

### System.Management.Automation.SwitchParameter
If this is set, then the operation returns immediately after submitting the request to CloudFormation.
If not set, then the operation is followed to completion, with stack events being output to the console.

### System.String
The Amazon Resource Name (ARN) of an AWS Identity and Access Management (IAM) role that AWS CloudFormation assumes to create the stack.
AWS CloudFormation uses the role's credentials to make calls on your behalf.
AWS CloudFormation always uses this role for all future operations on the stack.
As long as users have permission to operate on the stack, AWS CloudFormation uses this role even if the users don't have permission to pass it.
Ensure that the role grants least privilege.If you don't specify a value, AWS CloudFormation uses the role that was previously associated with the stack.
If no role is available, AWS CloudFormation uses a temporary session that is generated from your user credentials.

### System.String
The name that is associated with the stack.
The name must be unique in the Region in which you are creating the stack.A stack name can contain only alphanumeric characters (case sensitive) and hyphens.
It must start with an alphabetic character and cannot be longer than 128 characters.

### System.String
The AWS access key for the user account.
This can be a temporary access key if the corresponding session token is supplied to the -SessionToken parameter.

### Amazon.Runtime.AWSCredentials
An AWSCredentials object instance containing access and secret key information, and optionally a token for session-based credentials.

### System.String
The endpoint to make CloudFormation calls against.

The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
Only specify this parameter if you must direct the call to a specific custom endpoint, e.g.
if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.

### System.Management.Automation.PSCredential
Used with SAML-based authentication when ProfileName references a SAML role profile.
Contains the network credentials to be supplied during authentication with the configured identity provider's endpoint.
This parameter is not required if the user's default network identity can or should be used during authentication.

### System.String
Used to specify the name and location of the ini-format credential file (shared with the AWS CLI and other AWS SDKs)

If this optional parameter is omitted this cmdlet will search the encrypted credential file used by the AWS SDK for .NET and AWS Toolkit for Visual Studio first.
If the profile is not found then the cmdlet will search in the ini-format credential file at the default location: (user's home directory)\.aws\credentials.

If this parameter is specified then this cmdlet will only search the ini-format credential file at the location given.

As the current folder can vary in a shell or during script execution it is advised that you use specify a fully qualified path instead of a relative path.

### System.String
The user-defined name of an AWS credentials or SAML-based role profile containing credential information.
The profile is expected to be found in the secure credential file shared with the AWS SDK for .NET and AWS Toolkit for Visual Studio.
You can also specify the name of a profile stored in the .ini-format credential file used with the AWS CLI and other AWS SDKs.

### System.Object
The system name of an AWS region or an AWSRegion instance.
This governs the endpoint that will be used when calling service operations.
Note that the AWS resources referenced in a call are usually region-specific.

### System.String
The endpoint to make S3 calls against.

S3 is used by these cmdlets for managing S3 based templates and by the packager for uploading code artifacts and nested templates.

The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
Only specify this parameter if you must direct the call to a specific custom endpoint, e.g.
if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.

### System.String
The AWS secret key for the user account.
This can be a temporary secret key if the corresponding session token is supplied to the -SessionToken parameter.

### System.String
The session token if the access and secret keys are temporary session-based credentials.

### System.String
The endpoint to make STS calls against.

STS is used only if creating a bucket to store oversize templates and packager artifacts to get the caller account ID to use as part of the generated bucket name.

The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
Only specify this parameter if you must direct the call to a specific custom endpoint, e.g.
if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.

## OUTPUTS

### Firefly.CloudFormation.Model.CloudFormationResult
## NOTES

## RELATED LINKS

[[Resource Import]]((https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/resource-import.html))

