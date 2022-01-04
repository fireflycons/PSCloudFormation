---
title: Export - NestedStack
schema: 2.0.0
uid: tf-nested
---
# Working with Nested Stacks

Where nested stacks are involved, [Export-PSCFNTerraform](xref:Export-PSCFNTerraform) has two modes of operation.

1. The default is to import all nested stacks as `aws_cloudformation_stack` resources, thus leaving the nested stacks owned by CloudFormation.
1. Read all nested stacks and export these to HCL as modules. Enable this with the `-ExportNestedStacks` cmdlet parameter.

When exporting nested stacks, each `AWS::CloudFormation::Stack` resource found in the deployed stack is read and exported to a module in the `./modules` directory of the workspace. The modules take the names of the nested stacks, so will be something like `VpcStack-AGGQ9RLPLCGZ`. Each module (including root) that is found to contain nested stacks will gain a file `module_imports.tf` that declares the submodules. All references to and from the submodules will be resolved where possible.

As noted in [caveats](xref:tf-caveats#nested-stack-import), multiple `AWS::CloudFormation::Stack` resources using the same template source will result in multiple modules being exported.

See the [nested stacks example](xref:tf-example-nested).