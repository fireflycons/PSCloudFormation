---
uid: packaging
title: Packaging
---
# Packaging

The cmdlets are designed to take the pain out of packaging externally referenced assets when deploying CloudFormation. When using the CLI, packaging is a multi-step process involving zipping up artifacts that require it (e.g. lambda code), then pushing them to S3 (which can mostly be done with `aws cloudformation package`) then finally deploying the stack.

Using PSCloudFormation and its private bucket, all this is handled behind the scenes allowing you to focus on template and code developement with a quick turnaround for deploying changes.

As per `aws cloudformation package` the following packageable resources ae handled by PSCloudFormation:

* `AWS::ApiGateway::RestApi`
* `AWS::Serverless::Api`
* `AWS::Lambda::Function`
* `AWS::Serverless::Function`
* `AWS::AppSync::GraphQLSchema`
* `AWS::AppSync::Resolver`
* `AWS::Include`
* `AWS::ElasticBeanstalk::ApplicationVersion`
* `AWS::CloudFormation::Stack`
* `AWS::Glue::Job`
* `AWS::StepFunctions::StateMachine`

In the case of `AWS::CloudFormation::Stack` the process is recursive so you can have any level of stack nesting, and any of the above resources found in nested stacks are also packaged.

Starting with Release 4.0.13, there is no longer the need to explicitly package templates that require it with [New-PSCFNPackage](xref:New-PSCFNPackage). [New-PSCFNStack](xref:New-PSCFNStack) and [Update-PSCFNStack](xref:Update-PSCFNStack) check the template first for any resources that require packaging and then run the packaging step prior to submitting the template to CloudFormation. This automatic packging step makes use of PSCloudFormation's [private bucket](xref:private-bucket), and does not allow you to specify bucket, key prefix or additional S3 metadata. Should you require this level of control, then use [New-PSCFNPackage](xref:New-PSCFNPackage) to pre-package the template.

**CAVEAT** Whilst [New-PSCFNPackage](xref:New-PSCFNPackage) gives you full control over the use of S3, if you pipe the output of it directly into [New-PSCFNStack](xref:New-PSCFNStack) or [Update-PSCFNStack](xref:Update-PSCFNStack), these cmdlets lose the ability to provide dynamic parameter support for the template's parameters. This is due to how PowerShell dynamic parameters work. The dynamic parameter system needs to be able to see the content of the template at the time you are entering the command at the command line. The template content is not yet known as it's being piped from `New-PSCFNPackage` at run time. You can do one of the following:

* Create a new template from the packager with `-OutputTemplateFile` and subsequently run the create or update using the re-written template as the input.
* Supply all stack parameters in a [parameter file](xref:parameter-files).

Packaging of lambdas is quite advanced (see [Lambda Packaging](xref:lambda_packager)), and packaging of nested stacks is well tested as I use both frequently in my day job. Other resources are not so well tested, so please [raise an issue](https://github.com/fireflycons/PSCloudFormation/issues) if you find any bugs!

## Packaging Examples

Given a template `template.yaml` containing a nested template resource with simplified excerpt below, it can be deployed three ways. The examples below show creation of a stack, however the technique is the same for both Update and Reset.

```yaml
AWSTemplateFormatVersion: 2010-09-09
Parameters:
  VpcId:
    Type: AWS::EC2::VPC::Id
Resources:
  NestedStack:
    Type: AWS::CloudFormation::Stack
    Properties:
      TemplateUri: ./nested.yaml
      # etc...
```

### Directly with New-PSCFNPackage

The fact that the template contains file system references rather than S3 references will be detected and the packaging system invoked.
The nested template will be zipped and uploaded to the [private bucket](xref:private-bucket), the input template rewritten to a temporary file with the S3 location of the nested template inserted and the modifed template deployed. Following deployment the temporary copy of `template.yaml` is cleaned up.

```powershell
New-PSCFNPackage -StackName test-stack -TemplateLocation template.yaml -VpcId vpc-12345678
```

### Single Step with New-PSCFNPackage

In this case we are able to target a different bucket, and apply S3 metadata to any uploaded artifacts. The input template is still written to a temporary file and cleaned up after the deployment, howerver here it is necessary to use a [parameter file](xref:parameter-files) to pass a value for `VpcId` due to the nature of PowerShell's [dynamic parameter system](xref:dynamic-parameters).

```powershell
New-PSCFNPackage -TemplateFile template.yaml `
                 -S3Bucket another-bucket `
                 -S3Prefix test-stack-artifacts `
                 -Metadata @{ key1 = 'value1'; key2 = 'value2' } |
New-PSCFNStack -StackName test-stack -ParameterFile parameters.yaml
```

### Two Step with New-PSCFNPackage

This way we can leverage control over S3 and use dynamic parameters for the template parameters, however it needs to be run as two separate commands rather than chaining them with the PowerShell pipeline. The processed template is written to a new file `packaged.yaml` and this processed template is then deployed.

```powershell
New-PSCFNPackage -TemplateFile template.yaml `
                 -S3Bucket another-bucket `
                 -S3Prefix test-stack-artifacts `
                 -Metadata @{ key1 = 'value1'; key2 = 'value2' } `
                 -OutputTemplateFile packaged.yaml

New-PSCFNStack -StackName test-stack -TemplateLocation packaged.yaml -VpcId vpc-12345678
```
