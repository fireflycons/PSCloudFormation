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
* User Data import - `terraform import` seems to make a hash (literally) of user data properties imported from launch configurations (and probably instances too). You will need to correct that in the generated HCL or `terraform apply` is going to wipe out your user data script!<br/> Having said that, many user data scripts contain CloudFormation specific commands such as calls to `cfn-init` and references to CloudFormation specific pseudo-parameters which may hold little relevance in a terraform-managed stack.<br/>Where user data is found, a warning is issued.
* Lambda
    * Embedded function code declared in the CloudFormation Template with `ZipFile` will be extracted to a local file and hooked up to its `aws_lambda_function` resource by way of the [ArthurHlt/Zipper](https://registry.terraform.io/providers/ArthurHlt/zipper/latest) provider. Beware that code residing in the template _may well not be_ the current version of the code, as it nay have been updated at the console or by way of lambda APIs for updating function configuration since initial stack deployment.
    * Where code is located in S3 or ECR, terraform import does not pull back the location data, however this cmdlet will dscover and fix this up when emitting the HCL configuration.

## Imported Resources

* The existing resources in a deployed CloudFormation Stack are imported. If the CloudFormation Template that deployed the stack contains conditional resources which may not be instantiated at the time this tool is run, those resources won't be imported to Terraform. If you want the full functionality of the original CloudFormation template, that has to be added manually.

## Other Quirks

#### S3
* `aws_s3_bucket_policy` - Due to some strange persmissions thing, policy is nearly always imported as an empty string. You will have to manually add in the policy statement.<br/>Where bucket policies are found, a warning will be emitted.
