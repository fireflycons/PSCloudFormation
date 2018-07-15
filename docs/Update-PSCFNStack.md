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
Update-PSCFNStack [-StackName] <String> -TemplateLocation <String> [-Capabilities <String>] [-Wait] [-Force]
 [<CommonParameters>]
```

## DESCRIPTION
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
Update-PSCFNStack -StackName MyStack -TemplateLocation https://s3-eu-west-1.amazonaws.com/mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16
```

As per the first example, but with the template located in S3.

### EXAMPLE 3
```
Update-PSCFNStack -StackName MyStack -TemplateLocation s3://mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16
```

As per the first example, but using an S3 URL.
Caveat to this mechanism is that you must have a default region set in the curent shell.
The bucket is assumed to be in this region and the stack will also be built in this region.

### EXAMPLE 4
```
Update-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16 -Force
```

As per the first example, but it begins the update without you being asked to confirm the change

## PARAMETERS

### -StackName
Name for the new stack.

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

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
    You can pipe the stack name or ARN to this function

## OUTPUTS

### System.String
    ARN of the stack

## NOTES
This cmdlet genenerates additional dynamic command line parameters for all parameters found in the Parameters block of the supplied CloudFormation template

## RELATED LINKS
