# Importing Resources

Resource Import is a new feature of CloudFormation as of November 2019. This allows the following:
* Detach a resource from an existing stack.
* Import a resource that is not part of a stack into a stack.
* Migrate a resource between stacks (combination of the above two operations).

## How to Detach a Resource

1. Open the CloudFormation template
1. For the resources you want to detach, you should ensure these are not referenced by other resources within the stack that you wish to retain (i.e. `Ref`, `DependsOn` etc, or indirect references).
1. For the resources you want to detach, add the `DeletionPolicy` attribute with value of `Retain`.
```yaml
AWSTemplateFormatVersion: 2010-09-09
Resources:
  TestUser:
    Type: AWS::IAM::User
    Properties:
      Path: /

  ApplicationBucket:
    Type: AWS::S3::Bucket
    DeletionPolicy: Retain
    Properties:
      BucketName: import-bucket-123456789012
```
4. Update the stack
1. Remove the resource from your template
```yaml
AWSTemplateFormatVersion: 2010-09-09
Resources:
  TestUser:
    Type: AWS::IAM::User
    Properties:
      Path: /
```
6. Update the stack again.

At this point, the resource `import-bucket-123456789012` still exists, but are no longer part of the orginal stack.

## How to Import a Resource (using PSCloudFormation)

1. Create a resource import file. This is an array of resource descriptions as JSON or YAML. Each resource is described by the following properties
    1. `ResourceType` - The AWS type of the imported resource.
    1. `LogicalResourceId` - The logical name you are giving this resource in the stack you are importing into.
    1. `ResourceIdentifier` - A list of name-value pairs which identify the unattached physical resource. For instance, a bucket requires `BucketName` defined. Other resource may require additional properties.
```yaml
- ResourceType: AWS::S3::Bucket
  LogicalResourceId: ApplicationBucket
  ResourceIdentifier:
    BucketName: import-bucket-104552851521
```
2. Add the resource into the importing stack's template. Note how the properties in the above resource file map into the resource in the stack CloudFormation. A deletion policy attribute _must_ be specified on resources to import. This can be removed later with a further update to the stack if desired.
```yaml
AWSTemplateFormatVersion: 2010-09-09
Resources:
  TestUser:
    Type: AWS::IAM::User
    Properties:
      Path: /

  ApplicationBucket:
    Type: AWS::S3::Bucket
    DeletionPolicy: Retain
    Properties:
      BucketName: import-bucket-104552851521
```
3. Use `Update-PSCFNStack` to update the stack, giving the path to your resource import file as argument to `-ResourcesToImport`

```
PS H:\Dev\Git\PSCloudFormation> Update-PSCFNStack -StackName test-import-stack -TemplateLocation tests\resource-import\test-import-stack.yaml -ResourcesToImport tests\resource-import\test-import-bucket.yaml -Capabilities CAPABILITY_IAM -Wait


Creating change set PSCloudFormation-1575322246 for test-import-stack

Action LogicalResourceId ResourceType    Replacement PhysicalResourceId
------ ----------------- ------------    ----------- ------------------
Import ApplicationBucket AWS::S3::Bucket             import-bucket-104552851521


Begin update of test-import-stack now?

[Y] Yes [N] No [?] Help (default is "Yes"):
Updating stack test-import-stack
Waiting for update to complete
TimeStamp StackName         Logical ID                               Status                                        Status Reason
--------- ---------         ----------                               ------                                        -------------
21:31:01  test-import-stack test-import-stack                        IMPORT_IN_PROGRESS                            User Initiated
21:31:04  test-import-stack ApplicationBucket                        IMPORT_IN_PROGRESS                            Resource import started.
21:31:05  test-import-stack ApplicationBucket                        IMPORT_COMPLETE                               Resource import completed.
21:31:06  test-import-stack ApplicationBucket                        UPDATE_IN_PROGRESS                            Apply stack-level tags to imported resource if applicable.
21:31:26  test-import-stack ApplicationBucket                        UPDATE_COMPLETE                               -
21:31:27  test-import-stack test-import-stack                        IMPORT_COMPLETE                               -
arn:aws:cloudformation:eu-west-1:104552851521:stack/test-import-stack/bae2a440-154a-11ea-b2ac-069dc79e2b34
```
## Further Reading
https://aws.amazon.com/blogs/aws/new-import-existing-resources-into-a-cloudformation-stack/