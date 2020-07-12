param
(
    [string]$Test = 'Import'
)

import-module (Join-Path $PSScriptRoot 'PSCloudFormation.psd1')

<#
New-PSCFNPackage1 -TemplateFile "H:\Dev\Git\PSCloudFormation\tests\packager\complex-nested-stacks\base-stack.json"


Read-Host -Prompt "Press Enter" | Out-Null
exit 0
#>

$stackName = 'test-stack-CDF37043'

# Random names for resources to create
$bucketName = "bucket-$([Guid]::NewGuid())"
$paramName = "param-$([Guid]::NewGuid())"

# CloudFormation Template
$importTestTemplate = @"
AWSTemplateFormatVersion: 2010-09-09
Description: Set up a bucket so it can be filled and cause a DELETE_FAILED

Parameters:
  BucketName:
    Type: String
    Default: ""
  ParameterName:
    Type: String

Conditions:
  CreateBucket: !Not
    - !Equals
      - !Ref BucketName
      - ""

Resources:

  Bucket:
    Condition: CreateBucket
    DeletionPolicy: Delete
    Type: AWS::S3::Bucket
    Properties:
      BucketName: !Ref BucketName
  BasicParameter:
    Type: AWS::SSM::Parameter
    Properties:
      Name: !Sub "/pscloudformation/test/`${ParameterName}"
      Type: String
      Value: date
      Description: SSM Parameter for running date command.
      AllowedPattern: "^[a-zA-Z]{1,10}$"
"@

# Resource Imports
$resourcesToImport = @"
- ResourceType: AWS::S3::Bucket
  LogicalResourceId: Bucket
  ResourceIdentifier:
    BucketName: $bucketName
"@

# Parameters
$parameterFile = @"
- ParameterKey: BucketName
  ParameterValue: $bucketName
- ParameterKey: ParameterName
  ParameterValue: $paramName
"@

try
{
    # Tests RetainResources, Import, Parameters from file and waiting for previous operation to complete on update.

    # Save template to a file to use later
    $templatePath = Join-Path $PSScriptRoot "import-test.yaml"
    $importTestTemplate | Out-File -FilePath $templatePath -Encoding ASCII

    $commands = @(
        # Create stack using parameters and template from string arguments
        { New-PSCFNStack1 -StackName $stackName -TemplateLocation $importTestTemplate -ParameterFile $parameterFile -Wait -Verbose }

        # Add object to bucket so CF can't delete it
        { Write-S3Object -BucketName $bucketName -Key import-test.yaml -File $templatePath | Out-Null }
        {
            try
            {
                # Delete the stack, which will fail due to non-empty bucket
                # Note that you can't use -RetainResources on a stack that is not in DELETE_FAILED state. AWS will throw an exception.
                Remove-PSCFNStack1 -StackName $stackName -Wait -Force
            }
            catch
            {
                # Swallow expected DELETE_FAILED exception
            }
        }
        # Reset the stack, telling the delete operation to skip the bucket, using the file copy of the template for recreation.
        # Do not -Wait on the reset to test the following update waits for the stack to recreate.
        # Note that the delete operation waits regardless as we cannot create again till delete completes. Create will return immediately
        { Reset-PSCFNStack1 -StackName $stackName -RetainResources Bucket -TemplateLocation .\import-test.yaml -ParameterName $paramName -Force }

        # This update will track the create from above, then import the existing bucket
        { Update-PSCFNStack1 -StackName $stackName -UsePreviousTemplate -BucketName $bucketName -ResourcesToImport $resourcesToImport -Wait -Force }

        # Empty the bucket
        { Remove-S3Object -BucketName $bucketName -Key import-test.yaml -Force | Out-Null }

        # Delete everything to clean up
        { Remove-PSCFNStack1 -StackName $stackName -Wait -Force }
    )

    $commands |
    Foreach-Object {
        Write-Host "PS C:\>$_"
        Invoke-Command -NoNewScope $_
        Write-Host
    }
}
catch
{
    Write-Host $_.Exception.Message
    Write-Host $_.ScriptStackTrace
}
finally
{
    Remove-Item $templatePath -ErrorAction SilentlyContinue
}

Read-Host -Prompt "Press Enter" | Out-Null
exit 0
