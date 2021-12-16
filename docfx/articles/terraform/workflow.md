---
title: Export Workflow
schema: 2.0.0
uid: tf-workflow
---

# Workflow

[Export-PSCFNTerraform](xref:Export-PSCFNTerraform) performs the following steps

1. Ensure the workspace directory set by the `-WorkspaceDirectory` is clear. If a state file exists at this location, you will be asked whether to overwrite it. If you decline, the cmdlet will exit.<br/>If you give the `-Force` argument, any existing state will be silently overwritten.<br/>If you do not want to overwrite an existing state, choose a new workspace directory.
1. The CloudFormation stack is parsed and analysed. If additional providers are needed besides `aws`, these are selected
1. An initial `main.tf` is output with the terraform and provider configuration blocks along with empty resource declarations for the resources found in the CloudFormation template.
1. `terraform init` is invoked to set up the providers.
1. For each mapped resouce, `terraform import` is invoked to build up the state file.
1. The state file is read and analysed alongside the object graph generated from parsing the CloudFormation in order to determine dependencies between resources, inputs and outputs.
1. Lambdas are analysed and their code locations fixed up (see [here](xref:tf-caveats#embedded-scripts-and-code-provisioners-lambda)).
1. The state file along with the fixed up dependencies is serialized to HCL in `main.tf`. If the CloudFormation template contains a `Mappings` section, this is serialized as a `locals` block, and `!FindInMap` references into it are resolved.
1. A `terraform.tfvars` file is created with values for all inputs as per the current parameter values of the exported CloudFormation Stack. Note that values for `NoEcho` variables must be provided on the command line to [Export-PSCFNTerraform](xref:Export-PSCFNTerraform).
1. The generated HCL is checked for consistency. Any warnings or errors are reported.
    1. `terraform fmt` is invoked to tidy the output. This may find some issues.
    1. `terraform validate` is invoked to check further for issues.
    1. `terraform plan` is invoked to check for resources that will be destroyed or replaced without manual intervention.
1. Summary is output to console.

