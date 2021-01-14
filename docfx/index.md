# PSCloudFormation

[Go to code](https://github.com/fireflycons/PSCloudFormation).

## What is it?

When I began developing templates, I quickly became frustrated with the the length of time and complexity involved with deploying a change to a template to CloudFormation. A simple one-line fix would involve either using one of the command line tools, then jumping into the console to see how the update is progressing, or using the console itself and going through the half dozen or so screens just to get the thing uploaded and submitted.

When it came to more complex arrangements such as nested templates or lambda code, this just seemed to increase complexity tenfold. It could take hours to make a few changes to a template and get them successfully deployed.

Whilst primarily created as a development aid, there should be no reason why it shouldn't be included as part of an automated pipeline.

# Initial Design Goals

## Fast Feedback

First and foremost! You make a change, you want to learn quickly if it works. For this I wanted close parity with the AWS Tools CloudFormation commands, but crucially the ability to wait while the stack operation progresses and provide feedback directly to the console in the form of the stack event messages seen in the CloudFormation console in real time. This includes interleaving events from all nested stacks in the correct chronological order.

## Easy Template Parameters

With PoweShell's dynamic parameter system, I saw an opportunity to parse parameters from a template and provide them directly as command line arguments to the cmdlets in this module, thus saving the mucking about required to define stack parameters to either the AWS PowerShell cmdlets or `aws cloudformation deploy`. See the section on [dynamic parameters](xref:dynamic-parameters).

## A Solution to the Limitation on Template Size

When developing large templates, indeed if you're a fan of the JSON syntax, templates get very large very quickly. Once the template exceeds the 51,200 byte limit you are forced to use the console to pre-upload the template or upload directly to S3 and give an S3 URL, therefore adding more time to the development cycle. To solve this I came up with the idea of giving PSCloudFormation its own auto-created [private bucket](xref:private-bucket) such that an S3 bucket is automatically created when it is first needed and remains there for subsequent usage. Oversize templates are automatically uploaded to this bucket.

# Later Enhancements

As I started to create more lambdas and stacks with nested templates it quickly became apparent that there was room for improvement in this area. Having to manually package lambdas and upload them to S3 is a pain, `aws cloudformation package` is a pain, and trying to develop inline code lambdas is also a pain plus that limits thier scope!

After several iterations I think I've come up with a fairly seamless [packaging mechanism](xref:packaging) with additional support for [lambda packaging](xref:lambda_packager) that includes a lot of pre-flight checks.
