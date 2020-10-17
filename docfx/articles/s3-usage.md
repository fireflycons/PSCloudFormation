# PSCloudFormation and S3

The CloudFormation APIs have a limitation on the maximum size of a template that can be submitted without requiring it to be uploaded to S3 first. This is currently 51,200 bytes. To make life easier on the user, this library automatically manages this for you by creating and managing its own private bucket for oversize template uploads. This bucket is also leveraged by the packaging system (`New-PSCFNPackage`).

When S3 is required, this module will check for its private bucket and if not found, will attempt to create it.

## The Private S3 Bucket

The bucket is named as follows: `cf-templates-pscloudformation-REGION-ACCOUNTID` where `REGION` is the AWS Region you run the cmdlets in (e.g. eu-west-1) and `ACCOUNTID` is your AWS account number.

A lifecycle configuration to delete files older than 7 days is applied to prevent buildup of old temporary files. Note that if the caller does not have the correct permission to create lifecycle polices, a warning is displayed and the bucket is created without a policy.

### Required Permissions (Create)

If the bucket does not exist, then it is created when first needed. For bucket creation to be successful, the caller (IAM identity that runs the cmdlet) must have the following permissions

Required:

```
sts:GetCallerIdentity
s3:CreateBucket
```

Recommended:

```
s3:PutLifecycleConfiguration
```

### Required Permission (Use)

To use the bucket, the following permissions are required. Object level permissions can target the bucket directly.

```
sts:GetCallerIdentity
s3:GetObject
s3:PutObject
s3:ListBucket
```

