# PSCloudFormation
[![Build status](https://ci.appveyor.com/api/projects/status/fgt7d0icj7emc6hl/branch/master?svg=true)](https://ci.appveyor.com/project/fireflycons/pscloudformation/branch/master)

## How to Install

The module is published on the PowerShell Gallery and can be installed by following the instructions there.

### Windows PowerShell
![PowerShell Gallery](https://img.shields.io/powershellgallery/v/PSCloudFormation)

https://www.powershellgallery.com/packages/PSCloudFormation


### PowerShell Core (Linux)
![PowerShell Gallery](https://img.shields.io/powershellgallery/v/PSCloudFormation.netcore)

https://www.powershellgallery.com/packages/PSCloudFormation.netcore


## What it isn't

It isn't intended as a replacement for the raw API `New-CFNStack` etc. You should probably continue to use those in automation scripts, though there's nothing to stop you using this library to get more visual feedback but as always, ensure you test well before unleashing on production systems.

## What it is

It is two things really

1. A wrapper for the stack modification cmdlets of AWSPowerShell to make it easier to work with CloudFormation stacks from the command line, e.g. when developing a new stack.
2. An exercise in using PowerShell's dynamic cmdlet parameters.

I found that deploying stacks from the command line, whether via aws-cli or AWSPowerShell was extremely tedious, especially for stacks that have loads of parameters, both remembering all the parameters and having the continuously type them in, or edit _looong_ command lines in the command line history, along with frequent typos

So I thought to myself, I wonder how easy it would be to utilise the dynamic parameters feature of PowerShell to discover the parameters in a CloudFormation template and present these as arguments to some new stack manipulation cmdlets - and you got it - yes we can!

PowerShell builds a dynamic parameter set as soon as you give it an argument from which to work. An example you may be familiar with is the `Get-Content` cmdlet. As soon as you specify the argument for `-Path` it determines whether that path is a file system path (as opposed to registry, environment etc.) and then provides `-Raw` argument to read the file in its entirety rather than line-by-line.

With the cmdlets in this module, as soon as you tell them where the CloudFormation template is, they parse the template and extract all the parameters within and provide them as additional argments to the cmdlet, along with knowledge of whether they are required (no default), are constrained (AllowedValues, AllowedPattern), or typed (Number, `AWS::EC2::Subnet::Id` etc.) and perform pre-validation before submitting the stack update.

If a cmdlet is given the `-Wait` parameter then all stack events are output to the console, including events from any nested stacks with status values in colour so you can quickly spot what is going on.

## Module Cmdlets

All the cmdlets support the standard AWSPowerShell [Common Credential and Region Parameters](https://docs.aws.amazon.com/powershell/latest/reference/items/pstoolsref-commonparams.html).

For full syntax and some examples, use `Get-Help` on the module's cmdlets.

### Stack Modification Cmdlets

This module provides the following stack modification cmdlets

- `New-PSCFNStack` - ([Documentation](docs/en-US/New-PSCFNStack.md)) Create a new stack.
- `Update-PSCFNStack` - ([Documentation](docs/en-US/Update-PSCFNStack.md)) Update an existing stack.
- `Remove-PSCFNStack` - ([Documentation](docs/en-US/Remove-PSCFNStack.md)) Delete one or more existing stacks.
- `Reset-PSCFNStack` - ([Documentation](docs/en-US/Reset-PSCFNStack.md)) Delete, then redeploy an existing stack.

### Other Cmdlets

- `Get-PSCFNStackOutputs` ([Documentation](docs/en-US/Get-PSCFNStackOutputs.md)) Retrieves the outputs of a stack in various useful formats for use in creation of new stack templates that will use or import these values.

### Template Support

Oversize templates in your local file system (file size >= 51,200 bytes) are directly supported. They will be siliently uploaded to an S3 bucket which is created as necessary prior to processing. The bucket is named `cf-templates-pscloudformation-region-accountid` where
* `region` is the region you are building the stack in, e.g. `eu-west-1`.
* `accountid` is the numeric ID of the AWS account in which you are building the stack.

### Dynamic Template Parameter Arguments

As mentioned above, once the CloudFormation template location is known, it is parsed in the background and everything in the `Parameters` block of the template is extracted and turned into cmdlet arguments. Consider the following stack definition, saved as vpc.json

```json
{
    "AWSTemplateFormatVersion": "2010-09-09",
    "Parameters": {
        "VpcCidr": {
            "Description": "CIDR block for VPC",
            "Type": "String",
            "AllowedPattern": "^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\/([0-9]|[1-2][0-9]|3[0-2]))$"
        },
        "DnsSupport": {
            "Description": "Enable DNS Support",
            "Type": "String",
            "AllowedValues": [ "true", "false" ],
            "Default": "false"
        }
    },
    "Resources": {
        "Vpc": {
            "Type": "AWS::EC2::VPC",
            "Properties": {
                "CidrBlock": { "Ref": "VpcCidr" },
                "EnableDnsSupport": { "Ref": "DnsSupport" }
            }
        }
    }
}
```

If we wanted to create a new stack from this, we could do
```powershell
New-PSCFNStack -StackName MyVpc -TemplateLocation vpc.json -Wait -VpcCidr 10.0.0.0/16 -DnsSupport true
```

- Once we have given the `-TemplateLocation` argument and it points to an  existing file, we can just use regular powershell tab completion to discover the remaining arguments including `Wait`, the common credential arguments and the parameters read from the template
- The value you supply for `VpcCidr` will be asserted against the AllowedPattern regex _before_ the stack creation is initiated.
- The value for `DnsSupport` can be tab-completed between allowed values `false` and `true`

![New-PSCFNStack](images/New-PSCFNStack.gif?raw=true "New-PSCFNStack in action")

Were you to omit a required stack parameter, you will be prompted for it and the help text for the parameter is extracted from its description in the template file:

```powershell
New-PSCFNStack -StackName MyVpc -TemplateLocation .\vpc.json
```

```
cmdlet New-PSCFNStack at command pipeline position 1
Supply values for the following parameters:
(Type !? for Help.)
VpcCidr: !?
CIDR block for VPC
VpcCidr:
```

#### Update-PSCFNStack and Dynamic Argumments

When using ```Update-PSCFNStack ``` you only need to supply values on the command line for stack parameters you wish to change. All remaining stack paramaeters will assume their previous values.

# Notes

Thanks to

* [ramblingcookiemonster](http://ramblingcookiemonster.github.io/) for `New-DynamicParam` and build stuff.
* [Ahmad Gad](http://ahmad.jempress.com) for `Write-PSObject` that provides coloured output of stack events.
* [Antoine Aubry](https://github.com/aaubry/YamlDotNet) for `YamlDotNet`