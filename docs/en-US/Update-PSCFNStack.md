---
external help file: PSCloudFormation-help.xml
Module Name: PSCloudFormation
online version:
schema: 2.0.0
---

# Update-PSCFNStack

## SYNOPSIS
Updates a stack.

## SYNTAX

```
Update-PSCFNStack [-StackName] <String> [-TemplateLocation <String>] [-Capabilities <String>]
 [-NotificationARNs <String[]>] [-ResourceType <String[]>] [-RoleARN <String>]
 [-RollbackConfiguration <RollbackConfiguration>] [-Tag <Tag[]>] [-UsePreviousTemplate]
 [-ParameterFile <String>] [-ResourcesToImport <String>] [-Wait] [-Force] [-BackupTemplate]
 [<CommonParameters>]
```

## DESCRIPTION
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

## EXAMPLES

### EXAMPLE 1
```
Update-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16
```

Updates an existing stack of the same name or ARN from a local template file and waits for it to complete.
This template would have 'VpcCidr' defined within its parameter block
A changeset is created and displayed, and you are asked for confirmation befre proceeding.

### EXAMPLE 2
```
Update-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16 -BackupTemplate
```

As per the first example, but with the previous version of the template and its current parameter set saved to files in the current directory.

### EXAMPLE 3
```
Update-PSCFNStack -StackName MyStack -TemplateLocation https://s3-eu-west-1.amazonaws.com/mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16
```

As per the first example, but with the template located in S3.

### EXAMPLE 4
```
Update-PSCFNStack -StackName MyStack -TemplateLocation s3://mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16
```

As per the first example, but using an S3 URL.
Caveat to this mechanism is that you must have a default region set in the curent shell.
The bucket is assumed to be in this region and the stack will also be built in this region.

### EXAMPLE 5
```
Update-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16 -Force
```

As per the first example, but it begins the update without you being asked to confirm the change

## PARAMETERS

### -StackName
Name of the stack to update.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -TemplateLocation
Location of the template.
This may be
- Path to a local file
- s3:// URL pointing to template in a bucket
- https:// URL pointing to template in a bucket
Conditional: You must specify only TemplateLocationL, or set the UsePreviousTemplate to true.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Capabilities
If the stack requires IAM capabilities, TAB auctocompletes between the capability types.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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
Accept pipeline input: False
Accept wildcard characters: False
```

### -ResourceType
The template resource types that you have permissions to work with for this create stack action, such as AWS::EC2::Instance, AWS::EC2::*, or Custom::MyCustomInstance.
Use the following syntax to describe template resource types: AWS::* (for all AWS resource), Custom::* (for all custom resources), Custom::logical_ID (for a specific custom resource), AWS::service_name::* (for all resources of a particular AWS service), and AWS::service_name::resource_logical_ID (for a specific AWS resource).If the list of resource types doesn't include a resource that you're creating, the stack creation fails.
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
Accept pipeline input: False
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
Accept pipeline input: False
Accept wildcard characters: False
```

### -RollbackConfiguration
The rollback triggers for AWS CloudFormation to monitor during stack creation and updating operations, and for the specified monitoring period afterwards.

```yaml
Type: RollbackConfiguration
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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
Accept pipeline input: False
Accept wildcard characters: False
```

### -UsePreviousTemplate
Reuse the existing template that is associated with the stack that you are updating.
Conditional: You must specify only TemplateLocation, or set the UsePreviousTemplate to true.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ParameterFile
If present, path to a JSON file containing a list of parameter structures as defined for 'aws cloudformation create-stack'.
If a parameter of the same name is defined on the command line, the command line takes precedence.
If your stack has a parameter with the same name as one of the parameters to this cmdlet, then you *must* set the stack parameter via a parameter file.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ResourcesToImport
Specifices the path to a file (JSON or YAML) that declares existing resources to import into this CloudFormation Stack.
Requires AWSPowerShell \>= 4.0.1.0

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Wait
If set, wait for stack update to complete before returning.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
If set, do not ask for confirmation of the changeset before proceeding.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -BackupTemplate
If set, back up the current version of the template stored by CloudFormation, along with the current parameter set if any to files in the current directory.
This will assist with undoing any unwanted change.
Note that if you have dropped or replaced a database or anything else associcated with stored data, then the data is lost forever!

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
###     You can pipe the stack name or ARN to this function
## OUTPUTS

### System.String
###     ARN of the stack
## NOTES
This cmdlet genenerates additional dynamic command line parameters for all parameters found in the Parameters block of the supplied CloudFormation template

See also https://github.com/fireflycons/PSCloudFormation/blob/master/static/resource-import.md

## RELATED LINKS
