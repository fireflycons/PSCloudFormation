# PSCloudFormation
[![Build status](https://ci.appveyor.com/api/projects/status/fgt7d0icj7emc6hl/branch/master?svg=true)](https://ci.appveyor.com/project/fireflycons/pscloudformation/branch/master)

## Version 4 is here!

This version is a complete re-write in C#. I found that it was becoming a cumbersome beast keeping it in pure PowerShell, taking longer to load the module, and certain parts of it were running quite slowly.

Turning it into a binary module addresses the above problems and reduces complexity given the cmdlets share many common arguments meaning that inheritance can be used to reduce code duplication. It also gives me the chance to showcase three of my other projects: [Filrefly.CloudFormation](https://github.com/fireflycons/Firefly.CloudFormation) which underpins this module, [PSDynamicParameters](https://github.com/fireflycons/PSDynamicParameters) which is a library for managing PowerShell Dynamic Parameters for C# cmdlets and [CrossPlatformZip](https://github.com/fireflycons/CrossPlatformZip) which creates zip files targeting Windows or Linux/Unix/MacOS from any source platform - needed for the packaging component of this module. Lambda does not like zip files that don't contain Unix permission attributes!

### Breaking Changes

* Minimum requirement Windows PowerShell 5.1. All NetCore versions are supported.
* Requires modular [AWS.Tools](https://github.com/aws/aws-tools-for-powershell/issues/67). Monolithic AWSPowerShell is no longer supported (since PSCloudFormation v3.x).
* Meaning of `-Wait` parameter has changed. This only applies to `Update-PSCFNStack` and means that update should not begin if at the time the cmdlet is called, the target stack is found to be being updated by another process. In this case the update will wait for the other update to complete. All PSCloudFormation cmdlets will wait for their own operation to run to completion unless `-PassThru` is present.
* Return type from the cmdlets has changed. Instead of being just a stack status or an ARN, it is a structure containing both, defined [here](https://fireflycons.github.io/Firefly-CloudFormation/api/Firefly.CloudFormation.Model.CloudFormationResult.html).

### Enhancements

* More use of colour in changeset and stack event display.
* All properties of create, update and delete stack are now supported.
* More complete support for determining AWS credentials from all sources.
* [Resource Import](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/resource-import.html) supported (since v3.x) - still not supported by AWS.Tools cmdlets at time of writing.

### Gotchas

Due to the fact that the entire PowerShell process is a single .NET AppDomain, it is possible to fall into DLL hell. This module has various dependencies such as [YamlDotNet](https://github.com/aaubry/YamlDotNet). If something else in the current PowerShell session has loaded a different version of a dependent library like YamlDotNet, then you will get an assembly version clash when importing this module and the import will fail. Start a new PowerShell session and import there.

There is a way round this for pure .NET Core applications, but then I would have to target PowerShell Core only. The time isn't right for that yet, but if there's sufficient interest, then that could be the v5 release.

## How to Install

The module is published on the PowerShell Gallery and can be installed by following the instructions there.

Up until v3.x, there were two versions of this module published, one for Windows PowerShell and another for PowerShell Core for Linux/Mac. From now on, there is only the one module which works on all platforms.

The last version to support monolithic AWSPowerShell is v2.2.2 which can still be pulled from PSGallery.

### PowerShell (all platforms)
![PowerShell Gallery](https://img.shields.io/powershellgallery/v/PSCloudFormation)

https://www.powershellgallery.com/packages/PSCloudFormation



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
- `New-PSCFNPackage` ([Documentation](docs/en-US/New-PSCFNPackage.md)) Packages local artifacts, like `aws cloudformation package`

### Template Support

Oversize templates in your local file system (file size >= 51,200 bytes) are directly supported. They will be siliently uploaded to an S3 bucket which is created as necessary prior to processing with a delete after 7 days lifecycle policy to prevent buildup of rubbish. The bucket is named `ps-templates-pscloudformation-region-accountid` where
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

When using `Update-PSCFNStack` you only need to supply values on the command line for stack parameters you wish to change. All remaining stack paramaeters will assume their previous values.

#### Piped Template Body snd Dynamic Arguments

If you pass a template body to one of the cmdlets via the pipeline, it is not possible to use dynamic parameter arguments at all. The cmdlets will throw an exception saying that the cmdlet has no parameter of the given name. This is because at the time the dynamic parameter processing runs in the lifecycle of the cmdlet, the content of the template is not yet known, therefore the dynamic parameters cannot be built. You must in this case use a parameter file to define the template parameters (`-ParameterFile`)

# Notes

Thanks to

* [ramblingcookiemonster](http://ramblingcookiemonster.github.io/) for `PSDepend` and `PSDeploy` used in parts of the build of this project.
* [Antoine Aubry](https://github.com/aaubry/YamlDotNet) for `YamlDotNet`