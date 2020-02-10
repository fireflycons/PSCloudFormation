# Release Notes

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