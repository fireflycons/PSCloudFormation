---
title: Export - NestedStack
schema: 2.0.0
uid: tf-example-nested
---
# Example - Nested Stacks

This example takes the completed nested stacks example from [cfn101 workshop](https://cfn101.workshop.aws/intermediate/templates/nested-stacks.html) and exports it as modules to Terraform.

Demonstrates the following

* That each `AWS::CloudFormation::Stack` in the root stack is exported to a Terraform module.
* That the module blocks for the exported modules are correctly linked up with refrences to input variables and other module outputs.

## Inputs

The following CloudFormation templates form the nested stack:

* [Root Stack](xref:tf-example-nested-root-cf)
* [VPC Stack](xref:tf-example-nested-vpc-cf)
* [EC2 Stack](xref:tf-example-nested-ec2-cf)
* [IAM Stack](xref:tf-example-nested-iam-cf)

The stacks were deployed with a root stack name of `cfn-workshop-nested-stack`.


## Import run

This is the command to export `cfn-workshop-nested-stack` to Terraform in action...

![Running](../../../../images/tf-nested-stack-import.gif)

A few points to note about the warnings here

* References to unsupported AWS pseudo parameters.
* The fact that the instance in `EC2Stack` contains `AWS::CloudFormation::Init` and user data.
* As a result of the above, the instance is scheduled for replacement.

This goes to show that importing a running EC2 instance to Terraform is not always possible, especially if that instance depends on CloudFormation init events, which don't happen in Terraform. Ultimately a new instance would need to be created using either remote provisioning or some other setup mechanism and the workload migrated to it.

## Generated Outputs

A directory structure is generated in the workspace as follows

<span style="font-family: Lucida Console, Courier New, monospace">


&#x251C; [main.tf](xref:tf-example-nested-main-tf)</br>
&#x251C; [module_imports.tf](xref:tf-example-nested-module-tf)</br>
&#x251C; [terraform.tfvars](xref:tf-example-nested-vars-tf)</br>
&#x2514; modules</br>
&nbsp;&nbsp;&#x251C; cfn-workshop-nested-stack-EC2Stack-KGXCZCF8MN3C</br>
&nbsp;&nbsp;&#x2502;&nbsp;&nbsp;&#x251C; [main.tf](xref:tf-example-nested-ec2-main-tf)</br>
&nbsp;&nbsp;&#x2502;&nbsp;&nbsp;&#x2514; [terraform.tfvars](xref:tf-example-nested-ec2-vars-tf)</br>
&nbsp;&nbsp;&#x251C; cfn-workshop-nested-stack-VpcStack-CEYRYZJTGP7X</br>
&nbsp;&nbsp;&#x2502;&nbsp;&nbsp;&#x251C; [main.tf](xref:tf-example-nested-vpc-main-tf)</br>
&nbsp;&nbsp;&#x2502;&nbsp;&nbsp;&#x2514; [terraform.tfvars](xref:tf-example-nested-vpc-vars-tf)</br>
&nbsp;&nbsp;&#x2514; cfn-workshop-nested-stack-IamStack-R7URHPP7NDAS</br>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&#x2514; [main.tf](xref:tf-example-nested-iam-main-tf)</br>
</span>