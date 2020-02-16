---
external help file: PSCloudFormation-help.xml
Module Name: PSCloudFormation
online version:
schema: 2.0.0
---

# New-PSCFNPackage

## SYNOPSIS
Create a deployment package a-la aws cloudformation package

## SYNTAX

### File (Default)
```
New-PSCFNPackage -TemplateFile <String> -S3Bucket <String> [-S3Prefix <String>] [-KmsKeyId <String>]
 -OutputTemplateFile <String> [-UseJson] [-ForceUpload] [-Metadata <Hashtable>] [-ProfileName <String>]
 [-EndpointUrl <String>] [-AccessKey <String>] [-SecretKey <String>] [-ProfileLocation <String>]
 [-SessionToken <String>] [-NetworkCredential <PSCredential>] [-Credential <AWSCredentials>] [-Region <String>]
 [<CommonParameters>]
```

### PassThru
```
New-PSCFNPackage -TemplateFile <String> -S3Bucket <String> [-S3Prefix <String>] [-KmsKeyId <String>] [-UseJson]
 [-ForceUpload] [-Metadata <Hashtable>] [-PassThru] [-ProfileName <String>] [-EndpointUrl <String>]
 [-AccessKey <String>] [-SecretKey <String>] [-ProfileLocation <String>] [-SessionToken <String>]
 [-NetworkCredential <PSCredential>] [-Credential <AWSCredentials>] [-Region <String>] [<CommonParameters>]
```

### Console
```
New-PSCFNPackage -TemplateFile <String> -S3Bucket <String> [-S3Prefix <String>] [-KmsKeyId <String>] [-UseJson]
 [-ForceUpload] [-Metadata <Hashtable>] [-Console] [-ProfileName <String>] [-EndpointUrl <String>]
 [-AccessKey <String>] [-SecretKey <String>] [-ProfileLocation <String>] [-SessionToken <String>]
 [-NetworkCredential <PSCredential>] [-Credential <AWSCredentials>] [-Region <String>] [<CommonParameters>]
```

## DESCRIPTION
Packages the local artifacts (local paths) that your AWS CloudFormation template references.
The command uploads local artifacts, such as source code for an AWS Lambda function or a Swagger file for an AWS API Gateway REST API, to an S3 bucket.
The command returns a copy of your template, replacing references to local artifacts with the S3 location where the command uploaded the artifacts.
Use this command to quickly upload local artifacts that might be required by your template.
After you package your template's artifacts, run one of the *-PSCFNStack cmdlets to deploy the returned template.

You can also pipe this command directly to New-PSCFNStack, Update-PSCFNStack and Delete-PSCFNStack, however due to the complexities of pipeline handling, any stack
parameters need to be passed using a parameter file.

## EXAMPLES

### EXAMPLE 1
```
New-PSCFNPackage -TemplateFile template.yaml -OutputTemplateFile converted-template.yaml -S3Bucket mybucket -S3Prefix mykey
```

Upload code artifacts to specified bucket and key, and write new template to given file

### EXAMPLE 2
```
New-PSCFNPackage -TemplateFile template.yaml -OutputTemplateFile converted-template.yaml -S3Bucket mybucket -S3Prefix mykey -PassThru | Update-PSCFNStack -StackName my-stack -Wait
```

Upload code artifacts to specified bucket and key, and use converted template to update a stack

## PARAMETERS

### -TemplateFile
The path where your AWS CloudFormation template is located.

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

### -S3Bucket
The name of the S3 bucket where this command uploads the artifacts that are referenced in your template.

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

### -S3Prefix
A prefix name that the command adds to the artifacts' name when it uploads them to the S3 bucket.
The prefix name is a path name (folder name) for the S3 bucket.

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

### -KmsKeyId
The ID of an AWS KMS key that the command uses to encrypt artifacts that are at rest in the S3 bucket.

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

### -OutputTemplateFile
The path to the file where the command writes the output AWS CloudFormation template.
If you don't specify a path, the command writes the template to the standard output.

```yaml
Type: String
Parameter Sets: File
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UseJson
Indicates whether to use JSON as the format for the output AWS CloudFormation template.
YAML is used by default.

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

### -ForceUpload
Indicates whether to override existing files in the S3 bucket.
Specify this flag to upload artifacts even if they match existing artifacts in the S3 bucket.
CAVEAT: MD5 checksums are used to compare the local and S3 versions of the artifact.
If the artifact is a zip file, then it will almost certainly be
uploaded every time as zip files contain datetimes (esp.
last access time) and other file metadata that may change from subsequent invocations of zip on the local artifacts.

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

### -Metadata
A map of metadata to attach to ALL the artifacts that are referenced in your template.

```yaml
Type: Hashtable
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Emits an object pointing to the packaged template which can be piped to stack modification cmdlets in plate of their -TemplateLocation parameter.
Note that if you need to pass parameters to the stack, then a parameter file must be used.

```yaml
Type: SwitchParameter
Parameter Sets: PassThru
Aliases:

Required: True
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Console
{{ Fill Console Description }}

```yaml
Type: SwitchParameter
Parameter Sets: Console
Aliases:

Required: True
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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### [string] If -OutputTemplateFile is not provided, then the output is the converted template.
### [PSCloudFomation.Packager.Package] If -PassThru is used.
## NOTES
https://github.com/aws/aws-extensions-for-dotnet-cli/blob/master/src/Amazon.Lambda.Tools/LambdaUtilities.cs

## RELATED LINKS
