---
uid: parameter-files
title: Parameter Files
---
# Parameter Files

Parameter files provide an alternative means of supplying parameters to the stack. They can be used in conjunction with or instead of dynamic command line arguments. If the same parameter is pesent in a file and on the command line, then the command line takes preference.

Parameter files may me in either JSON or YAML and the format is an array of ParameterKey/ParameterValue objects

## YAML Format

```yaml
- ParameterKey: VpcId
  ParameterValue: vpc-12345678
- ParameterKey: SubnetId
  ParameterValue: subnet-12345678
```

## JSON Format

```json
[
    {
        "ParameterKey": "VpcId",
        "ParameterValue": "vpc-12345678"
    },
    {
        "ParameterKey": "SubnetId",
        "ParameterValue": "subnet-12345678"
    }
]
```

