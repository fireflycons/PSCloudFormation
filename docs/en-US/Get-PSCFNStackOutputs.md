---
external help file: PSCloudFormation-help.xml
Module Name: PSCloudFormation
online version:
schema: 2.0.0
---

# Get-PSCFNStackOutputs

## SYNOPSIS
Get the outputs of a stack in various formats

## SYNTAX

### Mappings (Default)
```
Get-PSCFNStackOutputs [-StackName] <String[]> [-AsMappingBlock] [-ProfileName <String>] [-EndpointUrl <String>]
 [-AccessKey <String>] [-SecretKey <String>] [-ProfileLocation <String>] [-SessionToken <String>]
 [-NetworkCredential <PSCredential>] [-Credential <AWSCredentials>] [-Region <String>] [<CommonParameters>]
```

### Parameters
```
Get-PSCFNStackOutputs [-StackName] <String[]> [-AsParameterBlock] [-ProfileName <String>]
 [-EndpointUrl <String>] [-AccessKey <String>] [-SecretKey <String>] [-ProfileLocation <String>]
 [-SessionToken <String>] [-NetworkCredential <PSCredential>] [-Credential <AWSCredentials>] [-Region <String>]
 [<CommonParameters>]
```

### Exports
```
Get-PSCFNStackOutputs [-StackName] <String[]> [-AsCrossStackReferences] [-ProfileName <String>]
 [-EndpointUrl <String>] [-AccessKey <String>] [-SecretKey <String>] [-ProfileLocation <String>]
 [-SessionToken <String>] [-NetworkCredential <PSCredential>] [-Credential <AWSCredentials>] [-Region <String>]
 [<CommonParameters>]
```

## DESCRIPTION
This function can be used to assist creation of new CloudFormation templates
that refer to the outputs of another stack.

It can be used to generate either mapping or prarameter blocks based on these outputs
by converting the returned object to JSON or YAML

## EXAMPLES

### EXAMPLE 1
```
Get-PSCFNStackOutputs -StackName MyStack -AsMappingBlock
```

When converted to JSON or YAML, can be pasted into the Mapping declaration of another template

### EXAMPLE 2
```
Get-PSCFNStackOutputs -StackName MyStack -AsParameterBlock
```

When converted to JSON or YAML, can be pasted into the Parameters declaration of another template

### EXAMPLE 3
```
Get-PSCFNStackOutputs -StackName MyStack -AsCrossStackReferences
```

When converted to JSON or YAML, provides a collection of Fn::Import stanzas that can be individually pasted into a new template

## PARAMETERS

### -StackName
One or more stacks to process.
One object is produced for each stack

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -AsMappingBlock
If set (default), returned object is formatted as a CloudFomration mapping block.
Converting the output to JSON or YAML renders text that can be pasted within a Mappings declararion.

```yaml
Type: SwitchParameter
Parameter Sets: Mappings
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsParameterBlock
If set, returned object is formatted as a CloudFormation parameter block.
Converting the output to JSON or YAML renders text that can be pasted within a Parameters declararion.

```yaml
Type: SwitchParameter
Parameter Sets: Parameters
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsCrossStackReferences
If set, returned object is formatted as a set of Fn::ImportValue statements, with any text matching the
stack name within the output's ExportName being replaced with a placeholder generated from the stack name with the word 'Stack' appended.
Make this a parameter to your new stack.

Whilst the result converted to JSON is not much use as it is, the individual elements can
be copied and pasted in where an Fn::ImportValue for that parameter would be used.

YAML is not currently supported for this operation.

```yaml
Type: SwitchParameter
Parameter Sets: Exports
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -AccessKey
The AWS access key for the user account.
This can be a temporary access key if the corresponding session token is supplied to the -SessionToken parameter.

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

### -Credential
An AWSCredentials object instance containing access and secret key information, and optionally a token for session-based credentials.

```yaml
Type: AWSCredentials
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndpointUrl
The endpoint to make the call against.

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

### -NetworkCredential
'Used with SAML-based authentication when ProfileName references a SAML role profile.
Contains the network credentials to be supplied during authentication with the configured identity provider's endpoint.
This parameter is not required if the user's default network identity can or should be used during authentication.

```yaml
Type: PSCredential
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProfileLocation
Used to specify the name and location of the ini-format credential file (shared with the AWS CLI and other AWS SDKs)

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

### -ProfileName
The user-defined name of an AWS credentials or SAML-based role profile containing credential information.
The profile is expected to be found in the secure credential file shared with the AWS SDK for .NET and AWS Toolkit for Visual Studio.
You can also specify the name of a profile stored in the .ini-format credential file used with the AWS CLI and other AWS SDKs.

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

### -Region
The system name of the AWS region in which the operation should be invoked.
For example, us-east-1, eu-west-1 etc.

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

### -SecretKey
The AWS secret key for the user account.
This can be a temporary secret key if the corresponding session token is supplied to the -SessionToken parameter.

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

### -SessionToken
The session token if the access and secret keys are temporary session-based credentials.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### [System.String[]] - You can pipe stack names or ARNs to this function
## OUTPUTS

### [PSObject] - An object dependent on the setting of the above switches. Pipe the output to ConvertTo-Json or ConvertTo-Yaml
## NOTES

## RELATED LINKS
