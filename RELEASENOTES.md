# Release Notes

# 4.1.16.4

* Enhancement - Employ the exported resource/attribute schema from `terraform-provider-aws` to correctly determine which attributes from the state file should be serialized to HCL.
* Enhancement - Replace community zip provider with hashicorp/archive for creation of lambda zip files.

# 4.1.16.3

* Enhancement - Terraform Export now pulls nested stacks as Terraform modules.

# 4.1.16.2

* Fix - Bump dependencies again due to another occurrence of the issue below on a different part of the template.

# 4.1.16.1 (Delisted)

* Fix - Exception thrown on creating new stack with nested stacks. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/126)

# 4.1.16.0

* Enhancement - Use new [CloudFormation Parser](https://github.com/fireflycons/Firefly.CloudFormationParser)
* Enhancement - Support AWS.Tools 4.1.16.0
* Enhancement - Rewrite [Terraform Export](https://fireflycons.github.io/PSCloudFormation/articles/terraform-export.html) to be a lot smarter due to now being able to fully parse CloudFormation.
* Fix - BucketName is a required property and must be set before making this call. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/123)

# 4.1.6.21

* Fix - Resizing vewport throws exception when running on a build agent.

# 4.1.6.20

* Fix - ...value of argument "maxLength" is out of range. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/122)

# 4.1.6.19

* Fix - Negative MinValue constraint on numeric parameter not working. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/120)
* Enhancement - Experimental release of Terraform Exporter. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/119)

# 4.1.6.18

* Fix - Unlabeled graph edges for changes to CreationPolicy, UpdatePolicy, Metadata. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/117)
* Enhancement - Display changeset time as UTC instead of ISO 8601. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/118)

# 4.1.6.17

* Fix - Handle Sequence contains no matching element when rendering SVG. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/116)

# 4.1.6.16

* Enhancement - Add graphical representation of changeset to browser view. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/112)

# 4.1.6.15

* Fix - Python lambda checker. Does not handle type hints in handler function. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/111)

# 4.1.6.14

* Fix - Incorrect handling of DisableRollback parameter (package update) [Issue link](https://github.com/fireflycons/Firefly.CloudFormation/issues/12)

# 4.1.6.13

* Enhancement - Show modification scopes in changeset detail. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/110)

# 4.1.6.12

* Fix - Exception when RDS delete creates a snapshot. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/109)

# 4.1.6.11

* Enhancement - Link with [SourceLink](https://github.com/dotnet/sourcelink/) enabled vesions of my own dependencies. No functional change. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/108)

# 4.1.6.10

* Enhancement - Add support for Python dependency resolution in lambda packager using `requirements.txt`. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/106)

# 4.1.6.9

* Fix - Null ref in New-PSCFNChangeset if no changes detected. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/105)

# 4.1.6.8

* Enhancement - Add argument completer for `-Select`. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/101)
* Fix - Session credentials not being picked up (introduced in 4.1.6.7). [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/102)
* Enhancement - Dump loaded AWS Assemblies in exception handler with `-Debug`. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/103)

# 4.1.6.7

* Tech/Enhancement - Remove dependency on `AWS.Tools.Common` thus removing the restriction that the module may only run with a specific vesion of `AWS.Tools`. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/99)
# 4.1.6.6

* Enhancement - Add new cmdlet `New-PSCFNChangeset`. Add mechanism to get detailed changeset information and view in browser, save to file or output to pipeline. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/96)

# 4.1.6.5

* Fix - Lambda handler detection fails if handler contains additional arguments (Python). [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/97)

# 4.1.6.4

* Enhancement - Add `-Select` parameter to stack modification cmdlets. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/94)

# 4.1.6.3

* Enhancement - ROLLBACK operations should be in red. [issue link](https://github.com/fireflycons/PSCloudFormation/issues/93)

# 4.1.6.2

* Fix - Parameters with SSM types should be included on command line. [issue link](https://github.com/fireflycons/PSCloudFormation/issues/92)

# 4.1.6.1

* Fix - Error message is unhelpful when stack requires capabilities. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/91)

# 4.1.6.0

* Technical - Update to AWS.Tools 4.1.6.0
* Enhancement - Support IncludeNestedStacks for changeset geneeration. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/89)

# 4.1.2.3

* Fix - Stack update/delete should proceed even when "broken". [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/88)

# 4.1.2.2

* Enhancement - Mechanism to cancel stack update. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/87)

# 4.1.2.1

* Fix - Incorporate Firefly.CloudFormation [issue 4](https://github.com/fireflycons/Firefly.CloudFormation/issues/4)
* Fix - Reinstate broken `-UsePreviousTemplate` argument. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/85)

# 4.1.2.0

* Technical - Upgrade to AWS.Tools v4.1.2.0 and align PSCloudFormation version with AWS Tools dependency

# 4.0.13

* Enhancement - Verify lambda handlers where possible. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/76).
* Enhancement - Create a documentation site. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/81).
* Fix - Python venv support not correct in Linux. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/82).

# 4.0.12

* Enhancement - Automatic packaging of templates that contain resources that require packaging, e.g. nested templates, lambdas with local filesystem references to code etc. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/77).
* Fix - Incorrect error message when a template file is not found. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/78).

# 4.0.11

* Fix - Python packager should gather dependencies that are a single file rather than module direcory, e.g. `six`. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/75).

# 4.0.10

* Fix - Deleting SAM template stack that fails to create throws exception. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/74).

# 4.0.9

* Fix - On running stack update, if there are no resources changed, e.g. only outputs have been changed, then exception is thrown. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/73).

# 4.0.8

* Fix - Incorrect generation of temporary directory name when packaging single file python lambda with dependencies. Masked until previous bug was fixed.

# 4.0.7

* Fix - Inverse logic in packaging for single file lambda without dependencies was causing a changed lambda to not be uploaded.

# 4.0.6

* Fix - Null reference exception when no template provided
* Fix - Python lambda packager with dependencies always uploading new version of package even when nothing has changed.

# 4.0.5

* Enhancement. Create a mechanism for packaging lambda dependencies. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/71).

# 4.0.4

* Fix: Packager is not checking whether packaged resource properties are required by CloudFomation and is incorrectly treating all as required. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/70).

# 4.0.3

* Fix -ForceS3. Setting not being passed to Firefly.CloudFormation. [Issue link](https://github.com/fireflycons/PSCloudFormation/issues/69).
* Fix exception thrown when Delete Stack is cancelled

# 4.0.2

* Bump version of Firefly.CloudFormation for this fix:
    * Address potential race condition when monitoring stack events during creation of set of nested stacks. It seems it's possible to retrieve a nested stack resource prior to it being assigned a physical resource ID.

# 4.0.1

* Packager should not zip a local artifact that is already zipped.

# 4.0.0

* Complete rewrite in C#
* Support for all CloudFormation properties
* Better authentication handling
* Some breaking changes - see README

## Version 4 Notes

This version is a complete re-write in C#. I found that it was becoming a cumbersome beast keeping it in pure PowerShell, taking longer to load the module, and certain parts of it were running quite slowly.

Turning it into a binary module addresses the above problems and reduces complexity given the cmdlets share many common arguments meaning that inheritance can be used to reduce code duplication. It also gives me the chance to showcase three of my other projects: [Firefly.CloudFormation](https://github.com/fireflycons/Firefly.CloudFormation) which underpins this module, [PSDynamicParameters](https://github.com/fireflycons/PSDynamicParameters) which is a library for managing PowerShell Dynamic Parameters for C# cmdlets and [CrossPlatformZip](https://github.com/fireflycons/CrossPlatformZip) which creates zip files targeting Windows or Linux/Unix/MacOS from any source platform - needed for the packaging component of this module. Lambda does not like zip files that don't contain Unix permission attributes!

### Breaking Changes

* Minimum requirement Windows PowerShell 5.1. All PowerShell Core versions are supported.
* Requires modular [AWS.Tools](https://github.com/aws/aws-tools-for-powershell/issues/67) - currently version `4.1.6.0` or higher. Monolithic AWSPowerShell is no longer supported (since PSCloudFormation v3.x). Future releases of this module will be version number aligned with the required version of `AWS.Tools` as and when enhancements are added in the space occupied by these cmdlets.
* Meaning of `-Wait` parameter has changed. This only applies to `Update-PSCFNStack` and means that update should not begin if at the time the cmdlet is called, the target stack is found to be being updated by another process. In this case the update will wait for the other update to complete. All PSCloudFormation cmdlets will wait for their own operation to run to completion unless `-PassThru` is present.
* Return type from the cmdlets has changed. Instead of being just a stack status or an ARN, it is a structure containing both, defined [here](https://fireflycons.github.io/Firefly-CloudFormation/api/Firefly.CloudFormation.Model.CloudFormationResult.html).

### Enhancements

* More use of colour in changeset and stack event display.
* All properties of create, update and delete stack are now supported.
* More complete support for determining AWS credentials from all sources.
* [Resource Import](https://fireflycons.github.io/PSCloudFormation/articles/resource-import.html) supported (since v3.x).
* [Dependency Packaging](https://fireflycons.github.io/PSCloudFormation/articles/lambda-packager.html) - For script based lambdas, it is possible to package dependent modules directly.
* Support for Python lambda dependency resultion via `requirements.txt`
* [Nested Changeset support](https://fireflycons.github.io/PSCloudFormation/articles/changesets.html) - With caveats! See documentation.
* [Changeset Detail view](https://fireflycons.github.io/PSCloudFormation/articles/changesets.html) - View changeset detail in browser.

### Gotchas

Due to the fact that the entire PowerShell process is a single .NET AppDomain, it is possible to fall into DLL hell. This module has various dependencies such as [YamlDotNet](https://github.com/aaubry/YamlDotNet). If something else in the current PowerShell session has loaded a different version of a dependent library like YamlDotNet, then you will get an assembly version clash when importing this module and the import will fail. Start a new PowerShell session and import there.

There is a way round this for pure .NET Core applications, but then I would have to target PowerShell Core only. The time isn't right for that yet, but if there's sufficient interest, then that could be the v5 release.

# 3.0.0

* Support for Resource Import
* Use modular AWS.Tools
* One module for Windows PowerShell and PowerShell Core

## 2.2.2

* Fix bug in nested stack packaging introduced by change in arguments to `New-PSCFNPackage`

## 2.2.1

* Automatically remove temporary template files created by `New-PSCFNPackage` when piped to stack modification cmdlets

## 2.2.0

* Add support for SAM templates - basically by adding support for CAPABILITY_AUTO_EXPAND.
* Fix a bug in `New-PSCFNPackage` where CodeUri entity incorrectly defined for `AWS::Serverless::Function`
* Enhance `New-PSCFNPackage` so that a packaged template may be piped directly to the stack modification cmdlets (with caveats - see notes on each cmdlet).

## 2.1.0

* Fix a pipeline leak in the code that waits for and reports progress causing failure detection to break
* All stack modification operations when run with `-Wait` now return the final status of the stack, e.g. `CREATE_COMPLETE` or the special value `NO_CHANGE` if updating and no change was detected. If an operation fails, then an exception is thrown with properties `Arn` and `Status` describing the stack and the final state. If `-PassThru` is set along with `-Wait` then the arn is returned on success.

## 2.0.0

* Adds support for stack refactoring - IMPORT operation. Requires AWSPowerShell >= 4.0.1.0 to work.
* Rearrange project structure.

## 1.8.0

* New cmdlet `New-PSCFNPackage`. Packages local artifacts to S3 as per `aws cloudformation package`
* Change all S3 bucket URLs to virtual host style
* Major workover to the tests.

## 1.7.1

* Minor bug fixes on `Reset-PSCFNStack`

## 1.7.0

* Make `Remove-PSCFNStack` ask unless `-Force` parameter present
* Give `Get-PSCFNStackOutputs` a `-AsHashtable` parameter to return the outputs as a raw format for processing by other commands.

## 1.6.0

10 May 2019

* Fix `Update-PSCFNStack` bug where new version of template has fewer template parameters than existing stack.

## v1.5.0

04 May 2019

* Add `-BackupTemplate` switch to `Update-PSCFNStack` and `Remove-PSCFNStack`. This saves the current state of the template and any paramaters to files in the current directory.
* Fix a bug unearthed with handling of `-ParameterFile` when testing the above.

## v1.4.0

01 May 2019

* Add tags to existing cloudformation bucket if they are not present

## v1.3.0

28 Apr 2019

* `Update-PSCFNStack` should name the stack when asking to proceed with update
* Tag S3 bucket when creating. When PSCloudFormation creates a bucket for processing large templates, it is tagged with info identifying its purpose.
* Support -EndpointUrl so that limited testing with localstack is possible. Add localstack tests.

## v1.2.0

24 Apr 2019

* `Update-PSCFNStack` - Don't throw if no changes detected. Only warn.
* `Update-PSCFNStack` - Don't throw if update cancelled. Only warn.
* Oversize templates - Generate a more unique S3 key.

## v1.1.0

23 Apr 2019

* Fixed issues with powershell-yaml in NetCore/Linux. YAML support now available

## v1.0.5

16 Apr 2019

* Handle the case where the shell has no AWS region default. This was causing exceptions to be thrown that did not really indicate what the issue was. Now the execption message more accurately identifies the issue with suggestions to correct the problem.
* Add this release notes file.
* Correct license link in manifest.
* Link this file in manifest.

## v1.0

14 Apr 2019

Add a new .NetCore target to provide additional PSGallery package 'PSCloudFormation.netcore` - Now supports PowerShell Core/Linux.

## v0.5

10 Apr 2019

* Improve format of stack event output and fix `SubString` bug in `Write-PSObject`
* Fix event timestamp bug. PowerShell API returns local times not UTC.
* Add `-ParameterFile` argument to support passing of stack parameters in a JSON file.

## v0.4

Log all cloudformation events to console (not only failures), including events from nested stacks. Use colour to indicate different states.

## v0.3.1

11 Mar 2019

Fixed a bug where the bucket name generated for pushing oversize templates to was not globally unique.

## v0.3

9 Mar 2019

Bugs and enhancements

* Handle large templates : bug
* Wait for rollback if update fails : bug
* Changeset should show resource action : enhancement
* `Update-PSCFNStack` not detecting newly added stack parameter : bug

## v0.2

9 Oct 2018

This release forwards the rest of the relevant command line arguments of `New-CFNStack` and `Update-CFNStack`, a notable omission being `-UsePreviousTemplate`.

## v0.1

25 Sep 2018

First release.