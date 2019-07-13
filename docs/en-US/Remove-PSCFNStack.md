---
external help file: PSCloudFormation-help.xml
Module Name: PSCloudFormation
online version:
schema: 2.0.0
---

# Remove-PSCFNStack

## SYNOPSIS
Delete one or more stacks.

## SYNTAX

```
Remove-PSCFNStack [-StackName] <String[]> [-Wait] [-Sequential] [-Force] [-BackupTemplate]
 [-ProfileName <String>] [-EndpointUrl <String>] [-AccessKey <String>] [-SecretKey <String>]
 [-ProfileLocation <String>] [-SessionToken <String>] [-NetworkCredential <PSCredential>]
 [-Credential <AWSCredentials>] [-Region <String>] [<CommonParameters>]
```

## DESCRIPTION
Delete one or more stacks.
If -Wait is specified, stack events are output to the console including events from any nested stacks.

Deletion of multiple stacks can be either sequential or parallel.
If deleting a gruop of stacks where there are dependencies between them
use the -Sequential switch and list the stacks in dependency order.

## EXAMPLES

### EXAMPLE 1
```
Remove-PSCFNStack -StackName MyStack
```

Deletes a single stack.

### EXAMPLE 2
```
Remove-PSCFNStack -StackName MyStack -BackupTemplate
```

As per the first example, but with the previous version of the template and its current parameter set saved to files in the current directory.

### EXAMPLE 3
```
'DependentStack', 'BaseStack' | Remove-PSCFNStack -Sequential
```

Deletes 'DependentStack', waits for completion, then deletes 'BaseStack'.

### EXAMPLE 4
```
'Stack1', 'Stack2' | Remove-PSCFNStack -Wait
```

Sets both stacks deleting in parallel, then waits for them both to complete.

### EXAMPLE 5
```
'Stack1', 'Stack2' | Remove-PSCFNStack
```

Sets both stacks deleting in parallel, and returns immediately.
See the CloudFormation console to monitor progress.

### EXAMPLE 6
```
Get-CFNStack | Remove-PSCFNStack
```

You would NOT want to do this, just like you wouldn't do rm -rf / !
It is for illustration only.
Sets ALL stacks in the region deleting simultaneously, which would probably trash some stacks
and then others would fail due to dependent resources.

## PARAMETERS

### -StackName
Either stack names or the object returned by Get-CFNStack, New-CFNStack, Update-CFNStack
and other functions in this module when run with -Wait.

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

### -Wait
If set and -Sequential is not set (so deleting in parallel), wait for all stacks to be deleted before returning.

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

### -Sequential
If set, delete stacks in the order they are specified on the command line or received from the pipeline,
waiting for each stack to delete successfully before proceeding to the next one.

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
If set, do not ask first.

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

### System.String[]
### You can pipe the names or ARNs of the stacks to delete to this function
## OUTPUTS

### System.String[]
### ARN(s) of deleted stack(s) else nothing if the stack did not exist.
## NOTES

## RELATED LINKS
