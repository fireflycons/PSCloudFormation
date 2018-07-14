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
Get-PSCFNStackOutputs [-StackName] <String[]> [-AsMappingBlock] [-AccessKey <String>] [-ProfileName <String>]
 [-Credential <AWSCredentials>] [-NetworkCredential <PSCredential>] [-SessionToken <String>]
 [-SecretKey <String>] [-Region <String>] [-ProfileLocation <String>] [<CommonParameters>]
```

### Parameters
```
Get-PSCFNStackOutputs [-StackName] <String[]> [-AsParameterBlock] [-AccessKey <String>] [-ProfileName <String>]
 [-Credential <AWSCredentials>] [-NetworkCredential <PSCredential>] [-SessionToken <String>]
 [-SecretKey <String>] [-Region <String>] [-ProfileLocation <String>] [<CommonParameters>]
```

### Exports
```
Get-PSCFNStackOutputs [-StackName] <String[]> [-AsCrossStackReferences] [-AccessKey <String>]
 [-ProfileName <String>] [-Credential <AWSCredentials>] [-NetworkCredential <PSCredential>]
 [-SessionToken <String>] [-SecretKey <String>] [-Region <String>] [-ProfileLocation <String>]
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
Get-PSCFNStackOutputs -StackName MyStack -AsParameterBlock
```

When converted to JSON, provides a collection of Fn::Import stanzas that can be individually pasted into a new template
YAML is currently not supported for this command

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
stack name within the output's ExportName being replaced with placeholder '${StackName}'.

Whilst the result converted to JSON or YAML is not much use as it is, the individual elements can
be copied and pasted in where an Fn::ImportValue for that parameter would be used.

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
    You can pipe stack names or ARNs to this function

## OUTPUTS

### PSObject
    An object dependent on the setting of the above switches. Pipe the output to ConvertTo-Json or ConvertTo-Yaml

## NOTES

## RELATED LINKS
