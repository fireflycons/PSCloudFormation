---
external help file: Firefly.PSCloudFormation.dll-Help.xml
Module Name: PSCloudFormation
online version:
schema: 2.0.0
---

# Get-PSCFNStackOutputs

## SYNOPSIS
Get the outputs of a stack in various formats

## SYNTAX

### Imports
```
Get-PSCFNStackOutputs [-AsCrossStackReferences] [-Format <String>] [-StackName] <String> [-AccessKey <String>]
 [-Credential <AWSCredentials>] [-EndpointUrl <String>] [-NetworkCredential <PSCredential>]
 [-ProfileLocation <String>] [-ProfileName <String>] [-Region <Object>] [-S3EndpointUrl <String>]
 [-SecretKey <String>] [-SessionToken <String>] [-STSEndpointUrl <String>] [<CommonParameters>]
```

### Hash
```
Get-PSCFNStackOutputs [-AsHashTable] [-StackName] <String> [-AccessKey <String>] [-Credential <AWSCredentials>]
 [-EndpointUrl <String>] [-NetworkCredential <PSCredential>] [-ProfileLocation <String>]
 [-ProfileName <String>] [-Region <Object>] [-S3EndpointUrl <String>] [-SecretKey <String>]
 [-SessionToken <String>] [-STSEndpointUrl <String>] [<CommonParameters>]
```

### Parameters
```
Get-PSCFNStackOutputs [-AsParameterBlock] -Format <String> [-StackName] <String> [-AccessKey <String>]
 [-Credential <AWSCredentials>] [-EndpointUrl <String>] [-NetworkCredential <PSCredential>]
 [-ProfileLocation <String>] [-ProfileName <String>] [-Region <Object>] [-S3EndpointUrl <String>]
 [-SecretKey <String>] [-SessionToken <String>] [-STSEndpointUrl <String>] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet can be used to assist creation of new CloudFormation templates that refer to the outputs of another stack.
It can be used to generate either mapping or parameter blocks based on these outputs by converting the returned object to JSON or YAML

## EXAMPLES

### EXAMPLE 1
```
$stackOutputs = Get-PSCFNStackOutputs1 -StackName my-stack -AsHashTable
```

Retrieves the outputs of the given stack as a hashtable of output key and output value.

## PARAMETERS

### -AsCrossStackReferences
If set, returned object is formatted as a set of Fn::ImportValue statements, with any text matching the stack name within the output's ExportName being replaced with a placeholder generated from the stack name with the word 'Stack' appended.
Make this a parameter to your new stack.

Whilst the result output is not much use as it is, the individual elements can be copied and pasted in where an Fn::ImportValue statements for that parameter would be used.

```yaml
Type: SwitchParameter
Parameter Sets: Imports
Aliases:

Required: True
Position: Named
Default value: False
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AsHashTable
If set (default), returned object is a hash table - key/value pairs for each stack output.

```yaml
Type: SwitchParameter
Parameter Sets: Hash
Aliases:

Required: True
Position: Named
Default value: False
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AsParameterBlock
If set, returned object is formatted as a CloudFormation parameter block.

```yaml
Type: SwitchParameter
Parameter Sets: Parameters
Aliases:

Required: True
Position: Named
Default value: False
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Format
Sets how output of parameters for CloudFormation template fragments should be formatted.

```yaml
Type: String
Parameter Sets: Imports
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

```yaml
Type: String
Parameter Sets: Parameters
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StackName
One or more stacks to process.
One object is produced for each stack

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
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

### System.Management.Automation.SwitchParameter
If set, returned object is formatted as a set of Fn::ImportValue statements, with any text matching the stack name within the output's ExportName being replaced with a placeholder generated from the stack name with the word 'Stack' appended.
Make this a parameter to your new stack.

Whilst the result output is not much use as it is, the individual elements can be copied and pasted in where an Fn::ImportValue statements for that parameter would be used.

### System.Management.Automation.SwitchParameter
If set (default), returned object is a hash table - key/value pairs for each stack output.

### System.Management.Automation.SwitchParameter
If set, returned object is formatted as a CloudFormation parameter block.

### System.String
Sets how output of parameters for CloudFormation template fragments should be formatted.

### System.String
One or more stacks to process.
One object is produced for each stack

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

### System.Collections.Hashtable
### System.String
## NOTES

## RELATED LINKS
