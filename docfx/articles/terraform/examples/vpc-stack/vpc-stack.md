---
title: Example - VPC Stack
schema: 2.0.0
uid: tf-example-vpc-stack
---

# Example - VPC Stack

This example shows a standard VPC setup with an IGW, two NAT gateways, two public and two private subnets and appropriate routes and NACLs.

Demonstrates the following
* CloudFormation `Mappings` are rendered as a `locals` block
* Inputs and outputs, along with `Terraform.tfvars` file holding current input values as read from CloudFormation
* Wide selection of intrinsics
    * `!Ref` is converted to input variable or resource reference expressions.
    * `!FindInMap` is converted to a reference to the appropriate value in `locals` block.
    * `!Sub` is converted to an interpolated string with implied reference expressions.
    * `!Cidr` and `!Join` are converted to corresponding Terraform built-in functions.
    * `!GetAZs` is converted to a reference to the `aws_availability_zone` data source.
    * `!Ref AWS::Region` is converted to a reference to the `aws_region` data source.
    * `!Select` applies an `[index]` to whatever list value is being referenced.
* What I am calling "merged resources", whereby there is a single Terraform resource that represents more than one CloudFormation resource are handled, e.g. `aws_internet_gateway` is a combination of `AWS::EC2::InternetGateway` and `AWS::EC2::VPCGatewayAttachement`

## CloudFormation

CloudFormation to build the stack is [here](./cloudformation.md).

The stack was deployed with a stack name of `test-vpc`

## Import run

This is the command to export `test-vpc` to Terraform in action...

![Running](../../../../images/vpc-import.gif)

## Generated outputs

* [main.tf](./hcl.md)
* [terraform.tfvars](./tfvars.md)

