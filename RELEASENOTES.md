# Release Notes

## CURRENT (v1.0.5)

16 Apr 2019

* Handle the case where the shell has no AWS region default. This was causing exceptions to be thrown that did not really indicate what the issue was. Now the execption message more accurately identifies the issue with suggestions to correct the problem.
* Add this release notes file.
* Correct license link in manifest
* Link this file in manifest

## v1.0

14 Apr 2019

Add a new .NetCore target to provide additional PSGallery package 'PSCloudFormation.netcore` - Now supports PowerShell Core/Linux

## v0.5

10 Apr 2019

* Improve format of stack event output and fix `SubString` bug in `Write-PSObject`
* Fix event timestamp bug. PowerShell API returns local times not UTC
* Add `-ParameterFile` argument to support passing of stack parameters in a JSON file

## v0.4

Log all cloudformation events to console (not only failures), including events from nested stacks. Use colour to indicate different states.

## v0.3.1

11 Mar 2019

Fixed a bug where the bucket name generated for pushinbg oversize templates to was not globally unique

## v0.3

9 Mar 2019

Bugs and enhancements

* Handle large templates : bug
* Wait for rollback if update fails : bug
* Changeset should show resource action : enhancement
* `Update-PSCFNStack` not detecting newly added stack parameter : bug

## v0.2

9 Oct 2018

This release forwards the rest of the relevant command line arguments of `New-CFNStack` and `Update-CFNStack`, a notable omission being `-UsePreviousTemplate`

## v0.1

25 Sep 2018

First release