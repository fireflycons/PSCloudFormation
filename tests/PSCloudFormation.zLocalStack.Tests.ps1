<#
    Run a few basic tests within the capabilities of LocalStack (https://github.com/localstack/localstack)
    This allows us to run tests without mocking out half of the AWSPowerShell library

    LocalStack restrictions
    - Doesn't deal well with YAML
    - Doesn't handle the stack changeset API so we can't run update tests

#>
$ModuleName = $(
    if ($PSVersionTable.PSEdition -ieq 'Core')
    {
        'PSCloudFormation.netcore'
    }
    else
    {
        'PSCloudFormation'
    }
)

$global:haveYaml = $null -ne (Get-Module -ListAvailable | Where-Object { $_.Name -ieq 'powershell-yaml' })

# http://www.lazywinadmin.com/2016/05/using-pester-to-test-your-manifest-file.html
# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = Get-ChildItem -Path (Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition)) -Recurse -Filter "$ModuleName.psd1" | Select-Object -ExpandProperty FullName

if (($ManifestFile | Measure-Object).Count -ne 1)
{
    throw "Cannot locate $ModuleName.psd1"
}

$global:TestStackArn = 'arn:aws:cloudformation:us-east-1:000000000000:stack/pester/00000000-0000-0000-0000-000000000000'
$global:UnchangedStackArn = 'arn:aws:cloudformation:us-east-1:000000000000:stack/unchanged/00000000-0000-0000-0000-000000000000'

$global:TestStackFilePathWithoutExtension = Join-Path $PSScriptRoot test-stack
$global:TestOversizeStackFilePath = Join-Path $PSScriptRoot test-oversize.json

# We need to pass some credentials, altough localstack doesn't care what they are
$global:localStackCommonParameters = @{
    AccessKey = 'AKAINOTUSED'
    SecretKey = 'notused'
    Region    = 'us-east-1'
}

function global:Reset-LocalStack
{
    Get-S3Bucket @localStackCommonParameters -EndpointUrl $localStackEndpoints.S3 |
    Select-Object -ExpandProperty BucketName |
    Foreach-Object {
        Remove-S3Bucket -BucketName $_ -DeleteBucketContent @localStackCommonParameters -EndpointUrl $localStackEndpoints.S3 -Force
    }

    Get-CFNStack @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF |
    Select-Object -ExpandProperty StackName |
    ForEach-Object {
        Remove-CFNStack -StackName $_ @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF -Force
    }

    while ((Get-CFNStack @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF | Measure-Object).Count -gt 0)
    {
        Start-Sleep -Seconds 1
    }
}


# Import the module and store the information about the module
Import-Module -Name $ManifestFile

# Look for LocalStack

foreach ($localstackUri in @('http://localhost:8080', 'http://localstack:8080'))
{
    try
    {
        Invoke-WebRequest -Uri $localstackUri -UseBasicParsing -TimeoutSec 1 | Out-Null
        break
    }
    catch
    {
        $localstackUri = $null
    }
}

if ($null -eq $localstackUri)
{
    Write-Warning "LocalStack unavailable. Integrated tests will be skipped"
    return
}
else
{
    Write-Host "LocalStack found at $localstackUri"


    Write-Host "Cleaning LocalStack resources"

    $localStackHost = ([Uri]$localstackUri).Host
    $global:localStackEndpoints = @{
        S3 = "http://$($localStackHost):4572"
        CF = "http://$($localStackHost):4581"
    }

    Reset-LocalStack
}

InModuleScope $ModuleName {

    Describe 'LocalStack Integrated Tests' {

        Mock -CommandName Get-STSCallerIdentity -MockWith {
            New-Object PSObject -Property @{
                Account = '000000000000'
            }
        }

        Mock -CommandName Get-EC2Region -MockWith {

            @(
                New-Object PSObject -Property @{ RegionName = "ap-south-1" }
                New-Object PSObject -Property @{ RegionName = "eu-west-3" }
                New-Object PSObject -Property @{ RegionName = "eu-west-2" }
                New-Object PSObject -Property @{ RegionName = "us-east-1" }
                New-Object PSObject -Property @{ RegionName = "ap-northeast-2" }
                New-Object PSObject -Property @{ RegionName = "ap-northeast-1" }
                New-Object PSObject -Property @{ RegionName = "sa-east-1" }
                New-Object PSObject -Property @{ RegionName = "ca-central-1" }
                New-Object PSObject -Property @{ RegionName = "ap-southeast-1" }
                New-Object PSObject -Property @{ RegionName = "ap-southeast-2" }
                New-Object PSObject -Property @{ RegionName = "eu-central-1" }
                New-Object PSObject -Property @{ RegionName = "us-east-1" }
                New-Object PSObject -Property @{ RegionName = "us-east-2" }
                New-Object PSObject -Property @{ RegionName = "us-west-1" }
                New-Object PSObject -Property @{ RegionName = "us-west-2" }
            )
        }

        Context 'S3 Bucket' {

            # These tests must be run in this order...

            It 'Should create oversize template bucket and tag it' {

                $credArgs = $localStackCommonParameters.Clone()
                $credArgs.Add('EndpointUrl', $global:localStackEndpoints.S3)

                $bucket = Get-CloudformationBucket -CredentialArguments $credArgs

                $bucket.BucketName | Should -Be 'cf-templates-pscloudformation-us-east-1-000000000000'

                Get-S3Bucket -BucketName $bucket.BucketName @localStackCommonParameters -EndpointUrl $global:localStackEndpoints.S3 | Should -Not -Be $null
                $tags = Get-S3BucketTagging -BucketName $bucket.BucketName @localStackCommonParameters -EndpointUrl $global:localStackEndpoints.S3
                $tags.Count | Should -Be 4
            }

            It 'Should tag untagged bucket' {

                $credArgs = $localStackCommonParameters.Clone()
                $credArgs.Add('EndpointUrl', $global:localStackEndpoints.S3)

                Remove-S3BucketTagging -BucketName 'cf-templates-pscloudformation-us-east-1-000000000000' @credArgs -Force

                $bucket = Get-CloudformationBucket -CredentialArguments $credArgs
                $tags = Get-S3BucketTagging -BucketName $bucket.BucketName @localStackCommonParameters -EndpointUrl $global:localStackEndpoints.S3
                $tags.Count | Should -Be 4
            }

            It 'Should not tag bucket if tags already present' {

                Mock -CommandName Write-S3BucketTagging -MockWith {}

                $credArgs = $localStackCommonParameters.Clone()
                $credArgs.Add('EndpointUrl', $global:localStackEndpoints.S3)

                Get-CloudformationBucket -CredentialArguments $credArgs | Out-Null
                Assert-MockCalled -CommandName Write-S3BucketTagging -Times 0 -Scope It
            }
        }

        Context 'Template Backup' {

            It 'Should create a template backup with parameter file' {

                New-CFNStack -StackName test-backup -TemplateBody (Get-Content -Raw "$($global:TestStackFilePathWithoutExtension).json") -Parameter @{ ParameterKey = 'VpcCidr'; ParameterValue = '10.0.0.0/16' } @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF

                $backupPathWithoutExtension = Join-Path $TestDrive "test-backup"

                $credArgs = $localStackCommonParameters.Clone()
                $credArgs.Add('EndpointUrl', $global:localStackEndpoints.CF)

                Save-TemplateBackup -StackName test-backup -OutputPath $TestDrive -CredentialArguments $credArgs

                Test-Path -Path "$($backupPathWithoutExtension).template.bak.json" | Should -Be $true
                Test-Path -Path "$($backupPathWithoutExtension).parameters.bak.json" | Should -Be $true
            }
        }

        Context 'New-PSCFNStack' {

            BeforeEach {

                # Clean out any resources
                Reset-LocalStack
            }

            It "Should create stack with valid command line arguments" {

                $arn = New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).json" @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF -Wait -VpcCidr 10.0.0.0/16
                { Get-CFNStack -StackName $arn @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF } | Should Not Throw
            }

            It "Should create stack with oversize template and valid command line arguments" {

                $arn = New-PSCFNStack -StackName oversize -TemplateLocation "$($global:TestOversizeStackFilePath)" @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF -Wait
                { Get-CFNStack -StackName $arn @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF } | Should Not Throw
            }
        }

        Context 'Remove-PSCFNStack' {

            BeforeEach {

                New-CFNStack -StackName test-delete -TemplateBody (Get-Content -Raw "$($global:TestStackFilePathWithoutExtension).json") -Parameter @{ ParameterKey = 'VpcCidr'; ParameterValue = '10.0.0.0/16' } @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF
                while ($true)
                {
                    $stack = Get-CFNStack -StackName test-delete  @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF

                    if ($stack.StackStatus -ieq 'CREATE_COMPLETE')
                    {
                        break
                    }

                    if ($stack.StackStatus -ieq 'CREATE_FAILED')
                    {
                        throw "Could not create test stack: $($stack.StackStatusReason)"
                    }
                }
            }

            AfterEach {

                Get-ChildItem -Path . -Filter "*.bak.json" | Remove-Item
            }

            It "Should delete a stack" {

                Remove-PSCFNStack -StackName test-delete @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF -Wait -Force

                { Get-CFNStack -StackName test-delete  @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF } | Should Throw
            }

            It "Should delete a stack and create template backup when requested, and recreate stack from backups" {

                Get-ChildItem -Path . -Filter "*.bak.json" | Measure-Object | Select-Object -ExpandProperty Count | Should -Be 0
                Remove-PSCFNStack -StackName test-delete @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF -BackupTemplate -Wait -Force

                { Get-CFNStack -StackName test-delete  @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF } | Should Throw
                Get-ChildItem -Path . -Filter "*.bak.json" | Measure-Object | Select-Object -ExpandProperty Count | Should -Be 2

                New-PSCFNStack -StackName test-delete @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF -TemplateLocation test-delete.template.bak.json -ParameterFile test-delete.parameters.bak.json -Wait

                { Get-CFNStack -StackName test-delete  @localStackCommonParameters -EndpointUrl $localStackEndpoints.CF } | Should Not Throw
            }
        }
    }
}