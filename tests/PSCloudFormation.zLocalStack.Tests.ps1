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

function global:Reset-LocalStack
{
    Get-S3Bucket -EndpointUrl $localStackEndpoints.S3 |
    Select-Object -ExpandProperty BucketName |
    Foreach-Object {
        Remove-S3Bucket -BucketName $_ -DeleteBucketContent -EndpointUrl $localStackEndpoints.S3 -Force
    }

    Get-CFNStack -EndpointUrl $localStackEndpoints.CF |
    Select-Object -ExpandProperty StackName |
    ForEach-Object {
        Remove-CFNStack -StackName $_ -EndpointUrl $localStackEndpoints.CF -Force
    }

    while ((Get-CFNStack -EndpointUrl $localStackEndpoints.CF | Measure-Object).Count -gt 0)
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
                New-Object PSObject -Property @{ RegionName = "eu-west-1" }
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

            It 'Should create oversize template bucket and tag it' {

                $bucket = Get-CloudformationBucket -CredentialArguments @{ Region = 'eu-west-1'; EndpointUrl = $global:localStackEndpoints.S3 }

                $bucket.BucketName | Should -Be 'cf-templates-pscloudformation-eu-west-1-000000000000'
                Assert-MockCalled Get-STSCallerIdentity
                Get-S3Bucket -BucketName $bucket.BucketName -EndpointUrl $global:localStackEndpoints.S3 | Should -Not -Be $null
                $tags = Get-S3BucketTagging -BucketName $bucket.BucketName -EndpointUrl $global:localStackEndpoints.S3
                $tags.Count | Should -Be 4

            }
        }

        Context 'New-PSCFNStack' {

            BeforeEach {

                # Clean out any resources
                Reset-LocalStack
            }

            It "Should create stack with valid command line arguments" {

                $arn = New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).json" -EndpointUrl $localStackEndpoints.CF -Wait -VpcCidr 10.0.0.0/16
                { Get-CFNStack -StackName $arn -EndpointUrl $localStackEndpoints.CF } | Should Not Throw
            }

            It "Should create stack with oversize template and valid command line arguments" {

                $arn = New-PSCFNStack -StackName oversize -TemplateLocation "$($global:TestOversizeStackFilePath)" -EndpointUrl $localStackEndpoints.CF -Wait
                { Get-CFNStack -StackName $arn -EndpointUrl $localStackEndpoints.CF } | Should Not Throw
            }
        }

        Context 'Remove-PSCFNStack' {

            It "Should delete a stack" {

                # Setup
                New-CFNStack -StackName test-delete -TemplateBody (Get-Content -Raw "$($global:TestStackFilePathWithoutExtension).json") -Parameter @{ ParameterKey = 'VpcCidr'; ParameterValue = '10.0.0.0/16' } -EndpointUrl $localStackEndpoints.CF

                while ($true)
                {
                    $stack = Get-CFNStack -StackName test-delete  -EndpointUrl $localStackEndpoints.CF

                    if ($stack.StackStatus -ieq 'CREATE_COMPLETE')
                    {
                        break
                    }

                    if ($stack.StackStatus -ieq 'CREATE_FAILED')
                    {
                        throw "Could not create test stack: $($stack.StackStatusReason)"
                    }
                }

                # test
                Remove-PSCFNStack -StackName test-delete -EndpointUrl $localStackEndpoints.CF -Wait

                { Get-CFNStack -StackName test-delete  -EndpointUrl $localStackEndpoints.CF } | Should Throw
            }
        }
    }
}