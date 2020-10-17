# Packaging

The cmdlets are designed to take the pain out of packaging externally referenced assets when deploying CloudFormation. When using the CLI, packaging is a multi-step process involving zipping up artifacts that equire it (e.g. lambda code), then pushing them to S3 (which can mostly be done with `aws cloudformation package`) then finally deploying the stack.

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

In the case of `AWS::CloudFormation::Stack` the process is recursive so you can have any level of stack nesting, and any of the avoce resources found in nested stacks are also packaged.

Starting with Release 4.0.13, there is no longer the need to explicitly package templates that require it with [New-PSCFNPackage](xref:New-PSCFNPackage). [New-PSCFNStack](xref:New-PSCFNStack) and [Update-PSCFNStack](xref:Update-PSCFNStack) check the template first for any resources that require packaging and then run the packaging step prior to submitting the template to CloudFormation. This automatic packging step makes use of PSCloudFormation's private bucket, and does not allow you to specify bucket, key prefix or additional S3 metadata. Should you require this level of control, then use [New-PSCFNPackage](xref:New-PSCFNPackage) to pre-package the template.

Packaging of lambdas is quite advanced (see [Lambda Packaging](xref:lambda_packager)), and packaging of nested stacks is well tested as I use both frequently in my day job. Other resources are not so well tested, so please [raise an issue](https://github.com/fireflycons/PSCloudFormation/issues) if you find any bugs!

