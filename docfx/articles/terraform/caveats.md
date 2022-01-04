---
title: Export - Caveats
schema: 2.0.0
uid: tf-caveats
---

# Terraform Export - Caveats

There will be more than what's here - I just haven't found them yet!

## Resource Types
* Currently this tool recognises the relationship between about 380 AWS resources and their Terraform equivalents. That's probably about half of what can actually be matched.
* Only about 30 of these are well tested thus far.
* There are still some AWS resources for which an equivalent resource has not yet been added to the Terraform AWS provider. `AWS::SecretsManager::SecretAttachment` at the time of writing and being one I use frequently with RDS stacks.

## Embedded scripts and code (provisioners, lambda)
* `AWS::CloudFormation::Init` data attached to resources is not exported because Terraform does not have this concept. Where init data is found, a warning is issued against the imported resource. Such scripts need refactoring to user data or some other mechanism.
* User Data import - `terraform import` seems to make a hash (literally) of user data properties imported from launch configurations and instances. This has the undesired side-effect of tainting the imported resource such that `plan` sees the resource as requiring replacement. You will need to correct that in the generated HCL or `terraform apply` is going to wipe out your resources!<br/> Having said that, many user data scripts contain CloudFormation specific commands such as calls to `cfn-init` and references to CloudFormation specific pseudo-parameters which may hold little relevance in a terraform-managed stack.<br/>Where user data is found, a warning is issued.
* Lambda
    * Embedded function code declared in the CloudFormation Template with `ZipFile` (`InlineCode` on a serverless function) will be extracted to a local file and hooked up to its `aws_lambda_function` resource by way of the [ArthurHlt/Zipper](https://registry.terraform.io/providers/ArthurHlt/zipper/latest) provider. Beware that code residing in the template _may well not be_ the current version of the code, as it may have been updated at the console or by way of lambda APIs for updating function configuration since initial stack deployment.
    * Where code is located in S3 or ECR, terraform import does not pull back the location data, however this cmdlet will discover and fix this up when emitting the HCL configuration.

## Imported Resources

The instantiated resources in a deployed CloudFormation Stack are imported. If the CloudFormation template that deployed the stack contains conditional resources which may not be instantiated at the time this tool is run, those resources won't be imported to Terraform. If you want the full functionality of the original CloudFormation template, that has to be added manually.

## Nested Stack Import

When [Export-PSCFNTerraform](xref:Export-PSCFNTerraform) is run with `-ExportNestedStacks` and the root stack contains more than one invocation of the same nested stack template with different parameters, then a separate Terraform module will be created for each instance of the nested stack. This is due to the fact that only *deployed* resources are imported and thus different invocations of the same template may produce a different number of resources, especially when conditions in the nested template may omit some resources.</br>It is therefore an exercise for the user to combine these into a single module.

## Custom Resources

Where custom resource invocations are found, a warning is generated. Usually stacks with custom resource invocations will also contain the lambdas that back them. These lambdas will be imported into the Terraform state.</br>With a bit of extra work it is possible to replicate the custom resource functionality in the Terraform configuration, but would require code changes to the lambdas in terms of inputs and outputs and an invocation of the updated lambda using the `aws_lambda_invocation` data source. Bear in mind that there may also be a Terraform provider or 3rd party module that provides the functionality of the custom resource.

## Other Quirks

#### S3
* `aws_s3_bucket_policy` - Due to some strange permissions thing, policy is nearly always imported as an empty string. You will have to manually add in the policy statement.<br/>Where bucket policies are found, a warning will be emitted.

## There will be bugs!

Please feel free to [raise issues](https://github.com/fireflycons/PSCloudFormation/issues) as and when you find them. Many of these are likely to be due to resources I haven't yet verified and will be because the import ID isn't correctly calculated or that the validation phase reports "Unconfigurable attribute".

When raising an issue, please provide the following

* If possible, a copy of the CloudFormation template or better still, create a reproduction of the error with a new template containing only the resource and conditions that cause the error, and supply this template.
* A full stack trace, which can be obtained by re-running the failing export with the `-Debug` swtich.