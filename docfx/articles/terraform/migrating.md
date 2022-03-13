---
title: Export - CavMigratingeats
schema: 2.0.0
uid: tf-migrating
---

# Migrating a CloudFormation Stack to Terraform

The general procedure for migrating a CloudFormation Stack is as follows

**Verify all these steps on a non-live copy of your production stack first - don't say I didn't warn you!**

1. Run [Export-PSCFNTerraform](xref:Export-PSCFNTerraform) on the stack, and make manual corrections. Test deploy your terraform configuration until you're sure it's correct, taking into account all [caveats](xref:tf-caveats).
1. Edit the CloudFormation Template and set a [DeletionPolicy](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-deletionpolicy.html) of `Retain` on all resources. Update the stack and *ensure that this gets applied*. In some cases you might need to force a resource update to one resouce (e.g. add a tag) for these policies to be applied. If the deletion policy isn't correctly applied, <span style="color: red">you will delete your resources</span>. Examine the updated template in the CloudFormation console first to be absolutely sure. </br>If working with nested stacks which you intend to export as modules, the deletion policy should be applied to all resources in all stacks in the nest *except* the `AWS::CloudFormation::Stack` resources, since we want the nested stacks to also be deleted whilst leaving their resources intact.
1. Delete the CloudFormation Stack. The stack definition will be removed from CloudFormation, however the resouces it contains will not. All resources should show `DELETE SKIPPED` in the event log in the CloudFormation console, and the operation should complete really quickly.
1. Now the stack resources are wholly owned by Terraform.

