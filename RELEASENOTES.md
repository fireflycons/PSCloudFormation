# Release Notes

# 4.0.9

* Fix - On running stack update, if there are no resources changed, e.g. only outputs have been changed, then exception is thrown.

# 4.0.8

* Fix - Incorrect generation of temporary directory name when packaging single file python lambda with dependencies. Masked until previous bug was fixed.

# 4.0.7

* Fix - Inverse logic in packaging for single file lambda without dependencies was causing a changed lambda to not be uploaded.

# 4.0.6

* Fix - Null reference exception when no template provided
* Fix - Python lamdba packager with dependencies always uploading new version of package even when nothing has changed.

# 4.0.5

* Enhancement. Create a mechanism for packaging lambda dependencies.

# 4.0.4

* Fix: Packager is not checking whether packaged resource properties are required by CloudFomation and is incorrectly treating all as required.

# 4.0.3

* Fix -ForceS3. Setting not being passed to Firefly.CloudFormation
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