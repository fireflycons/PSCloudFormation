# PSCloudFormation
[![Build status](https://ci.appveyor.com/api/projects/status/fgt7d0icj7emc6hl/branch/master?svg=true)](https://ci.appveyor.com/project/fireflycons/pscloudformation/branch/master)

This module depends on [AWS.Tools](https://docs.aws.amazon.com/powershell/latest/userguide/pstools-welcome.html) 4.1.6.0 which you should install/upgrade to first

Required AWS.Tools modules:

* AWS.Tools.Common
* AWS.Tools.CloudFormation
* AWS.Tools.S3

## Version 4 is here!

This version is a complete re-write in C#. I found that it was becoming a cumbersome beast keeping it in pure PowerShell, taking longer to load the module, and certain parts of it were running quite slowly.

Turning it into a binary module addresses the above problems and reduces complexity given the cmdlets share many common arguments meaning that inheritance can be used to reduce code duplication. It also gives me the chance to showcase three of my other projects: [Firefly.CloudFormation](https://github.com/fireflycons/Firefly.CloudFormation) which underpins this module, [PSDynamicParameters](https://github.com/fireflycons/PSDynamicParameters) which is a library for managing PowerShell Dynamic Parameters for C# cmdlets and [CrossPlatformZip](https://github.com/fireflycons/CrossPlatformZip) which creates zip files targeting Windows or Linux/Unix/MacOS from any source platform - needed for the packaging component of this module. Lambda does not like zip files that don't contain Unix permission attributes!

### New Documentation Site

Head over [here](https://fireflycons.github.io/PSCloudFormation/index.html) for further reading and more in-depth discussion on the featues of this module.

### Breaking Changes

* Minimum requirement Windows PowerShell 5.1. All PowerShell Core versions are supported.
* Requires modular [AWS.Tools](https://github.com/aws/aws-tools-for-powershell/issues/67) - currently version `4.1.6.0`. Monolithic AWSPowerShell is no longer supported (since PSCloudFormation v3.x). Future releases of this module will be version number aligned with the required version of `AWS.Tools`.
* Meaning of `-Wait` parameter has changed. This only applies to `Update-PSCFNStack` and means that update should not begin if at the time the cmdlet is called, the target stack is found to be being updated by another process. In this case the update will wait for the other update to complete. All PSCloudFormation cmdlets will wait for their own operation to run to completion unless `-PassThru` is present.
* Return type from the cmdlets has changed. Instead of being just a stack status or an ARN, it is a structure containing both, defined [here](https://fireflycons.github.io/Firefly-CloudFormation/api/Firefly.CloudFormation.Model.CloudFormationResult.html).

### Enhancements

* More use of colour in changeset and stack event display.
* All properties of create, update and delete stack are now supported.
* More complete support for determining AWS credentials from all sources.
* [Resource Import](https://fireflycons.github.io/PSCloudFormation/articles/resource-import.html) supported (since v3.x) - still not supported by AWS.Tools cmdlets at time of writing.
* [Dependency Packaging](https://fireflycons.github.io/PSCloudFormation/articles/lambda-packager.html) - For script based lambdas, it is possible to package dependent modules directly.
* [Nested Changeset support](https://fireflycons.github.io/PSCloudFormation/articles/changesets.html) - With caveats! See documentation.
* [Changeset Detail view](https://fireflycons.github.io/PSCloudFormation/articles/changesets.html) - View changeset detail in browser.

### Gotchas

Due to the fact that the entire PowerShell process is a single .NET AppDomain, it is possible to fall into DLL hell. This module has various dependencies such as [YamlDotNet](https://github.com/aaubry/YamlDotNet). If something else in the current PowerShell session has loaded a different version of a dependent library like YamlDotNet, then you will get an assembly version clash when importing this module and the import will fail. Start a new PowerShell session and import there.

There is a way round this for pure .NET Core applications, but then I would have to target PowerShell Core only. The time isn't right for that yet, but if there's sufficient interest, then that could be the v5 release.

## How to Install

The module is published on the PowerShell Gallery and can be installed by following the instructions there.

Up until v3.x, there were two versions of this module published, one for Windows PowerShell and another for PowerShell Core for Linux/Mac. From now on, there is only the one module which works on all platforms.

The last version to support monolithic AWSPowerShell is v2.2.2 which can still be pulled from PSGallery.

### PowerShell (all platforms)
[![PowerShell Gallery](https://img.shields.io/powershellgallery/v/PSCloudFormation)](https://www.powershellgallery.com/packages/PSCloudFormation)


## Module Cmdlets

All the cmdlets support the standard AWSPowerShell [Common Credential and Region Parameters](https://docs.aws.amazon.com/powershell/latest/reference/items/pstoolsref-commonparams.html).

For full syntax and some examples, use `Get-Help` on the module's cmdlets.

### Stack Modification Cmdlets

This module provides the following stack modification cmdlets

- `New-PSCFNStack` - ([Documentation](https://fireflycons.github.io/PSCloudFormation/cmdlets/New-PSCFNStack.html)) Create a new stack.
- `Update-PSCFNStack` - ([Documentation](https://fireflycons.github.io/PSCloudFormation/cmdlets/Update-PSCFNStack.html)) Update an existing stack.
- `Remove-PSCFNStack` - ([Documentation](https://fireflycons.github.io/PSCloudFormation/cmdlets/Remove-PSCFNStack.html)) Delete one or more existing stacks.
- `Reset-PSCFNStack` - ([Documentation](https://fireflycons.github.io/PSCloudFormation/cmdlets/Reset-PSCFNStack.html)) Delete, then redeploy an existing stack.

### Other Cmdlets

- `New-PSCFNChangeSet` - ([Documentation](https://fireflycons.github.io/PSCloudFormation/cmdlets/New-PSCFNChangeSet.html)) Create changeset only, for review.
- `Get-PSCFNStackOutputs` ([Documentation](https://fireflycons.github.io/PSCloudFormation/cmdlets/Get-PSCFNStackOutputs.html)) Retrieves the outputs of a stack in various useful formats for use in creation of new stack templates that will use or import these values.
- `New-PSCFNPackage` ([Documentation](https://fireflycons.github.io/PSCloudFormation/cmdlets/New-PSCFNPackage.html)) Packages local artifacts, like `aws cloudformation package`.

### Template Support

Oversize templates in your local file system (file size >= 51,200 bytes) are directly supported. They will be silently uploaded to an S3 bucket which is [created as necessary](https://fireflycons.github.io/PSCloudFormation/articles/s3-usage.html) prior to processing with a delete after 7 days lifecycle policy to prevent buildup of rubbish. The bucket is named `ps-templates-pscloudformation-region-accountid` where
* `region` is the region you are building the stack in, e.g. `eu-west-1`.
* `accountid` is the numeric ID of the AWS account in which you are building the stack.

### Dynamic Template Parameter Arguments

Once the CloudFormation template location is known, it is parsed in the background and everything in the `Parameters` block of the template is extracted and turned into cmdlet arguments. Read more [here](https://fireflycons.github.io/PSCloudFormation/articles/dynamic-parameters.html).


# Notes

Thanks to

* [ramblingcookiemonster](http://ramblingcookiemonster.github.io/) for `PSDepend` and `PSDeploy` used in parts of the build of this project.
* [Antoine Aubry](https://github.com/aaubry/YamlDotNet) for `YamlDotNet`
