---
external help file: Firefly.PSCloudFormation.dll-Help.xml
Module Name: PSCloudFormation
online version: https://github.com/fireflycons/PSCloudFormation/blob/master/static/lambda-dependencies.md
schema: 2.0.0
---

# New-PSCFNPackage

## SYNOPSIS
Packages the local artifacts (local paths) that your AWS CloudFormation template references, similarly to aws cloudformation package.

## SYNTAX

```
New-PSCFNPackage [-Metadata <IDictionary>] [-OutputTemplateFile <String>] [-S3Bucket <String>]
 [-S3Prefix <String>] [-TemplateFile] <String> [-AccessKey <String>] [-Credential <AWSCredentials>]
 [-EndpointUrl <String>] [-NetworkCredential <PSCredential>] [-ProfileLocation <String>]
 [-ProfileName <String>] [-Region <Object>] [-S3EndpointUrl <String>] [-SecretKey <String>]
 [-SessionToken <String>] [-STSEndpointUrl <String>] [<CommonParameters>]
```

## DESCRIPTION
The command uploads local artifacts, such as source code for an AWS Lambda function or a Swagger file for an AWS API Gateway REST API, to an S3 bucket.
The command returns a copy of your template, replacing references to local artifacts with the S3 location where the command uploaded the artifacts.
Unlike aws cloudformation package, the output template is in the same format as the input template, i.e.
there is no conversion from JSON to YAML.

Use this command to quickly upload local artifacts that might be required by your template.
After you package your template's artifacts, run the New-PSCFNStack command to deploy the returned template.

To specify a local artifact in your template, specify a path to a local file or folder, as either an absolute or relative path.
The relative path is a location that is relative to your template's location.

For example, if your AWS Lambda function source code is in the /home/user/code/lambdafunction/ folder, specify CodeUri: /home/user/code/lambdafunction for the AWS::Serverless::Function resource.
The command returns a template and replaces the local path with the S3 location: CodeUri: s3://mybucket/lambdafunction.zip.

If you specify a file, the command directly uploads it to the S3 bucket, zipping it first if the resource requires it (e.g.
lambda).
If you specify a folder, the command zips the folder and then uploads the .zip file.
For most resources, if you don't specify a path, the command zips and uploads the current working directory.
he exception is AWS::ApiGateway::RestApi; if you don't specify a BodyS3Location, this command will not upload an artifact to S3.

## EXAMPLES

### EXAMPLE 1
```
New-PSCFNPackage -TemplateFile my-template.json -OutputTemplateFile my-modified-template.json
```

Reads the template, recursively walking any AWS::CloudFormation::Stack resources, uploading code artifacts and nested templates to S3, using the bucket that is auto-created by this module.

### EXAMPLE 2
```
New-PSCFNPackage -TemplateFile my-template.json -OutputTemplateFile my-modified-template.json -S3Bucket my-bucket -S3Prefix template-resouces
```

Reads the template, recursively walking any AWS::CloudFormation::Stack resources, uploading code artifacts and nested templates to S3, using the specified bucket which must exist, and key prefix for all uploaded objects.

### EXAMPLE 3
```
New-PSCFNPackage -TemplateFile my-template.json | New-PSCFNStack -StackName my-stack -ParameterFile stack-parameters.json
```

Reads the template, recursively walking any AWS::CloudFormation::Stack resources, uploading code artifacts and nested templates to S3, then sends the modified template to New-PSCFNStack to create a new stack.

Due to the nuances of PowerShell dynamic parameters, any stack customization parameters must be placed in a parameter file, as PowerShell starts the New-PSCFNStack cmdlet before it receives the template, therefore the template parameters cannot be known in advance.

## PARAMETERS

### -Metadata
A map of metadata to attach to ALL the artifacts that are referenced in your template.

```yaml
Type: IDictionary
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -OutputTemplateFile
The path to the file where the command writes the output AWS CloudFormation template.
If you don't specify a path, the command writes the template to the standard output.

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

### -S3Bucket
The name of the S3 bucket where this command uploads the artifacts that are referenced in your template.
If not specified, then the oversize template bucket will be used.

```yaml
Type: String
Parameter Sets: (All)
Aliases: Bucket, BucketName

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -S3Prefix
A prefix name that the command adds to the artifacts' name when it uploads them to the S3 bucket.
The prefix name is a path name (folder name) for the S3 bucket.

```yaml
Type: String
Parameter Sets: (All)
Aliases: Prefix, KeyPrefix

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -TemplateFile
The path where your AWS CloudFormation template is located.

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

### System.Collections.IDictionary
A map of metadata to attach to ALL the artifacts that are referenced in your template.

### System.String
The path to the file where the command writes the output AWS CloudFormation template.
If you don't specify a path, the command writes the template to the standard output.

### System.String
The name of the S3 bucket where this command uploads the artifacts that are referenced in your template.
If not specified, then the oversize template bucket will be used.

### System.String
A prefix name that the command adds to the artifacts' name when it uploads them to the S3 bucket.
The prefix name is a path name (folder name) for the S3 bucket.

### System.String
The path where your AWS CloudFormation template is located.

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

## NOTES

## RELATED LINKS

[Packaging Lambda Dependencies](https://github.com/fireflycons/PSCloudFormation/blob/master/static/lambda-dependencies.md)

