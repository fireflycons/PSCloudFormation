# PSCloudFormation
[![Build status](https://ci.appveyor.com/api/projects/status/fgt7d0icj7emc6hl/branch/master?svg=true)](https://ci.appveyor.com/project/fireflycons/pscloudformation/branch/master)

A set PowerShell cmdlets for manipulating AWS CloudFormation stacks.

## Features

* Live display of stack events as a template is being applied when running synchronously (without `-PassThru` switch). Where nested stacks are involved, the events from these are also shown interleaved with those of the parent stack in chronlogical order.
* When using a workstation with a GUI, [detailed changeset information](https://fireflycons.github.io/PSCloudFormation/articles/changesets.html) can be brought up in a browser, including an SVG graph depicting the relationships between resources that are being modifed - like `terraform graph`
* Automatic packaging and upload to S3 of dependencies such as nested stack templates, lambdas, and other resources that require S3 references as described in [aws cloudformation package](https://docs.aws.amazon.com/cli/latest/reference/cloudformation/package.html)
* Close argument parity with similar cmdlets in AWS.Tools.CloudFormation

### Experimental Features

* Export a live CloudFormation Stack to Terraform HCL. This is sitll a work in progress, however it's still better than what you get simply by running [terraform import](https://www.terraform.io/docs/cli/import/index.html). See [Terrafom Export](https://fireflycons.github.io/PSCloudFormation/articles/terraform-export.html)</br>Now supports exporting nested stacks as Terraform modules.

## Dependencies

This module depends on [AWS.Tools](https://docs.aws.amazon.com/powershell/latest/userguide/pstools-welcome.html) version `4.1.16.0` or higher which you should install/upgrade to first

Required AWS.Tools modules:

* AWS.Tools.CloudFormation
* AWS.Tools.S3

## New Documentation Site

Head over [here](https://fireflycons.github.io/PSCloudFormation/index.html) for further reading and more in-depth discussion on the featues of this module.

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
- `Export-PSCFNTerraform` ([Documentation](https://fireflycons.github.io/PSCloudFormation/articles/terraform-export.html)) Exports a deployed CloudFormation stack to Terraform HCL.
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
* [Olivier Duhart](https://github.com/b3b00/csly) for `csly` used to parse python `METADATA` files.
* [Alexandre Rabérin](https://github.com/KeRNeLith/QuikGraph) for QuikGraph
* [Ian Webster](https://github.com/typpo/quickchart) and [QuickChart.io](https://quickchart.io/) for SVG rendering API.
