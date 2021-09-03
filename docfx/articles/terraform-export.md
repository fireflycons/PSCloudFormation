# Terraform Export

**Highly experimental!**

Given a scenario where your organisation has dictated that all native CloudFormation stacks should be migrated to Terraform, this cmdlet goes some way to easing the pain of that operation. Currently [terraform import](https://www.terraform.io/docs/cli/import/index.html) does not generate [HCL](https://www.terraform.io/docs/language/index.html) code for imported resources, only state information leaving the user to work out and add in all the attributes for every imported resource.

The [Export-PSCFNTerraform](xref:Export-PSCFNTerraform) cmdlet will read a CloudFormation Stack via the AWS CloudFormation Service and make a best attempt at generating HCL for the resources the stack contains. Additionally it will try to fix up dependencies between these resources and input variables (stack parameters) as best it can. Thus the amount of work to be done in getting the HCL for the stack correct is greatly reduced.

The general procedure for migrating a CloudFormation Stack is as follows

1. Run [Export-PSCFNTerraform](xref:Export-PSCFNTerraform) on the stack, and make manual corrections. Test deploy your terraform stack to another environment/region/account until you're sure it's correct.
1. Edit the CloudFormation Template and set a `DeletionPolicy` of `Retain` on all resources. Update the stack and ensure that this gets applied. In some cases you might need to force a resource update to one resouce (e.g. add a tag) for these policies to be applied.
1. Delete the CloudFormation Stack. The stack definition will be removed from CloudFormation, however the resouces it contains will not.
1. Now the stack resources are wholly owned by Terraform.

## Caveats

* Currently this tool recognises the relationship between about 380 AWS resources and their Terraform equivalents. That's probably about half of what can actually be matched.
* There are still some AWS resources for which an equivalent resource has not yet been added to the Terraform AWS provider. `AWS::SecretsManager::SecretAttachment` at the time of writing and being one I use frequently with RDS stacks.
* Dependency resolution between resources and parameters to resources is still an inexact science, done by resource ID matching.

## Future Improvements

* Increase the number of mapped resources.
* Create a more complete parser for CloudFormation that resolves intrinsics and build a directed graph between the elements of the stack, thus being able to properly parse relationships between inputs, resources and outputs
* Make the exporter recursive through nested stacks, producing a Terraform module for each nested stack.
* Learn GoLang and become a contributor to the Terraform AWS provider 😁

