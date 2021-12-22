param
(
    [string]$Test = 'Packager'
)

import-module (Join-Path $PSScriptRoot 'PSCloudFormation.psd1')

#New-PSCFNStack -StackName test-import -TemplateLocation C:\OneDrive\abm\OneDrive\Dev\Git\PSCloudFormation\ScratchPad\imports.yaml

#cd C:\OneDrive\abm\OneDrive\Dev\Spike\MinValueTest
#New-PSCFNStack -StackName min-value-test -TemplateLocation C:\OneDrive\abm\OneDrive\Dev\Spike\PSCFN-TestStacks\TestParameterValidation.yaml -ParamValue sssss -Debug


$doImport = $true

if ($doImport)
{
    $stacks = @(
        'fc-basestack'
        #'test-vpc'
        #'test-lambda'
        #'test-sg'
        #'fc-basestack-Vpc-AGGQ9RLPLCGZ'
        #'xrefmap-service'
        #'proget'
        #'photo-viewer'                     
        #'dead-mans-handle'
        #'torrent-instance'
        #'packer-role'
        #'dynamic-ip-watcher'
        #'fc-basestack-NatInstance-X7Y0C6YDEBWD'  #!Base64 render - ImportValue can't be rendered
        #'iam-user-ddns'
        #'redirect-to-linkedin'             # Lambda permissions not being wired to functions
        #'chef-test-kitchen'

    )
    $stacks |
    Foreach-Object {
        Export-PSCFNTerraform -StackName $_ -WorkspaceDirectory c:\temp\torrent-tf -Force -Debug
        Copy-Item "c:\temp\torrent-tf\terraform.tfstate" "c:\temp\torrent-tf\fc-$_.tfstate"
    }
}
else
{
    $stacks = @(
        'fc-test-vpc'
        #'fc-test-lambda'
        #'fc-test-sg'
        #'fc-fc-basestack-Vpc-AGGQ9RLPLCGZ'
        #'fc-dead-mans-handle'
        #'fc-xrefmap-service'
        #'fc-photo-viewer'
        #'fc-proget'
        #'fc-torrent-instance'
        #'fc-packer-role'
        #'fc-dynamic-ip-watcher'
        #'fc-fc-basestack-NatInstance-X7Y0C6YDEBWD'
        #'fc-iam-user-ddns'
        #'fc-redirect-to-linkedin'
        #'fc-chef-test-kitchen'

    )

    $stacks |
    Foreach-Object {
        Copy-Item "c:\temp\torrent-tf\$_.tfstate" "c:\temp\torrent-tf\terraform.tfstate"
        Export-PSCFNTerraform -StackName ($_.SubString(3)) -WorkspaceDirectory c:\temp\torrent-tf -Force -Debug
    }
}



#cd  C:\OneDrive\abm\OneDrive\Dev\Bitbucket\redirector-service
#.\venv\Scripts\Activate.ps1
#New-PSCFNStack -StackName redirector-service -Capabilities CAPABILITY_IAM,CAPABILITY_AUTO_EXPAND -TemplateLocation .\redirector-service.yaml -BindTargetGroup 0
#Update-PSCFNStack  -StackName redirector-service -Capabilities CAPABILITY_IAM,CAPABILITY_AUTO_EXPAND -IncludeNestedStacks -TemplateLocation .\redirector-service.yaml -BindTargetGroup 1
#Remove-PSCFNStack -StackName redirector-service

#cd C:\OneDrive\abm\OneDrive\Dev\Git\aws-nested-changeset-bug
#cd C:\OneDrive\abm\OneDrive\Dev\Spike\Serverless
#New-PSCFNStack -StackName nested-changeset-bug -TemplateLocation root-stack.yaml -DeployRule 0
#New-PSCFNChangeSet -StackName nested-changeset-bug -TemplateLocation root-stack.yaml -IncludeNestedStacks -ShowInBrowser -DeployRule 1
#Remove-PSCFNStack -StackName nested-changeset-bug
#.\venv\Scripts\Activate.ps1

#Update-PSCFNStack -StackName serverless-test -TemplateLocation serverless-test.yaml -Capabilities CAPABILITY_IAM,CAPABILITY_AUTO_EXPAND


Read-Host -Prompt "Press Enter" | Out-Null
exit 0


$stackName = 'test-stack-' + [Guid]::NewGuid().ToString().SubString(0,8)

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
    switch($Test)
    {
        'Import' {

            # Tests RetainResources, Import, Parameters from file and waiting for previous operation to complete on update.

            # Save template to a file to use later
            $tempTemplatePath = Join-Path $PSScriptRoot "import-test.yaml"
            $importTestTemplate | Out-File -FilePath $tempTemplatePath -Encoding ASCII

            $commands = @(
                # Create stack using parameters and template from string arguments
                { New-PSCFNStack1 -StackName $stackName -TemplateLocation $importTestTemplate -ParameterFile $parameterFile -Wait -Verbose }

                # Add object to bucket so CF can't delete it
                { Write-S3Object -BucketName $bucketName -Key import-test.yaml -File $tempTemplatePath | Out-Null }
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
        }

        'Packager' {
        
            # TODO: Make these assets available via build and compute path
            $nestedTemplatePath = "H:\Dev\Git\PSCloudFormation\tests\Firefly.PSCloudFormation.Tests.Unit\Resources\DeepNestedStack\base-stack.json"

            $commands = @(
            
                { 
                    New-PSCFNPackage1 -TemplateFile $nestedTemplatePath -S3Bucket fireflycons-misc-eu-west-1 -S3Prefix nested-test -Debug -Verbose |
                    New-PSCFNStack1 -StackName $stackName -Capabilities CAPABILITY_IAM
                }

                { Read-Host -Prompt "Press Enter" | Out-Null }

                { Remove-PSCFNStack1 -StackName $stackName -Force }
            )

        }
    }

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
    if ($tempTemplatePath)
    {
        Remove-Item $tempTemplatePath -ErrorAction SilentlyContinue
    }
}

Read-Host -Prompt "Press Enter" | Out-Null
exit 0
