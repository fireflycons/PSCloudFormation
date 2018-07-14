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
Remove-PSCFNStack [-StackName] <String[]> [-Wait] [-Sequential] [-AccessKey <String>] [-ProfileName <String>]
 [-Credential <AWSCredentials>] [-NetworkCredential <PSCredential>] [-SessionToken <String>]
 [-SecretKey <String>] [-Region <String>] [-ProfileLocation <String>] [<CommonParameters>]
```

## DESCRIPTION
Delete one or more stacks.

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
'DependentStack', 'BaseStack' | Remove-PSCFNStack -Sequential
```

Deletes 'DependentStack', waits for completion, then deletes 'BaseStack'.

### EXAMPLE 3
```
'Stack1', 'Stack2' | Remove-PSCFNStack -Wait
```

Sets both stacks deleting in parallel, then waits for them both to complete.

### EXAMPLE 4
```
'Stack1', 'Stack2' | Remove-PSCFNStack
```

Sets both stacks deleting in parallel, and returns immediately.
See the CloudFormation console to monitor progress.

### EXAMPLE 5
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

### -AccessKey
a help message

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
a help message

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

### -NetworkCredential
a help message

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
a help message

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
a help message

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
a help message

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
a help message

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
a help message

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
    You can pipe the names or ARNs of the stacks to delete to this function

## OUTPUTS

### None

## NOTES

## RELATED LINKS
