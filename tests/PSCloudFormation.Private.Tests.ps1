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

# http://www.lazywinadmin.com/2016/05/using-pester-to-test-your-manifest-file.html
# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = Get-ChildItem -Path (Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition)) -Recurse -Filter "$ModuleName.psd1" | Select-Object -ExpandProperty FullName

if (($ManifestFile | Measure-Object).Count -ne 1)
{
    throw "Cannot locate $ModuleName.psd1"
}

Import-Module -Name $ManifestFile

$global:templatePathJson = Join-Path $PSScriptRoot test-stack.json
$global:templatePathYaml = Join-Path $PSScriptRoot test-stack.yaml
$global:paramPath = Join-Path $PSScriptRoot test-params.json

$global:azs = @(
    New-Object PSObject -Property @{ Region = 'ap-south-1'; ZoneName = @('ap-south-1a', 'ap-south-1b') }
    New-Object PSObject -Property @{ Region = 'eu-west-3'; ZoneName = @('eu-west-3a', 'eu-west-3b', 'eu-west-3c') }
    New-Object PSObject -Property @{ Region = 'eu-west-2'; ZoneName = @('eu-west-2a', 'eu-west-2b', 'eu-west-2c') }
    New-Object PSObject -Property @{ Region = 'eu-west-1'; ZoneName = @('eu-west-1a', 'eu-west-1b', 'eu-west-1c') }
    New-Object PSObject -Property @{ Region = 'ap-northeast-2'; ZoneName = @('ap-northeast-2a', 'ap-northeast-2c') }
    New-Object PSObject -Property @{ Region = 'ap-northeast-1'; ZoneName = @('ap-northeast-1a', 'ap-northeast-1c', 'ap-northeast-1d') }
    New-Object PSObject -Property @{ Region = 'sa-east-1'; ZoneName = @('sa-east-1a', 'sa-east-1c') }
    New-Object PSObject -Property @{ Region = 'ca-central-1'; ZoneName = @('ca-central-1a', 'ca-central-1b') }
    New-Object PSObject -Property @{ Region = 'ap-southeast-1'; ZoneName = @('ap-southeast-1a', 'ap-southeast-1b', 'ap-southeast-1c') }
    New-Object PSObject -Property @{ Region = 'ap-southeast-2'; ZoneName = @('ap-southeast-2a', 'ap-southeast-2b', 'ap-southeast-2c') }
    New-Object PSObject -Property @{ Region = 'eu-central-1'; ZoneName = @('eu-central-1a', 'eu-central-1b', 'eu-central-1c') }
    New-Object PSObject -Property @{ Region = 'us-east-1'; ZoneName = @('us-east-1a', 'us-east-1b', 'us-east-1c', 'us-east-1d', 'us-east-1e', 'us-east-1f') }
    New-Object PSObject -Property @{ Region = 'us-east-2'; ZoneName = @('us-east-2a', 'us-east-2b', 'us-east-2c') }
    New-Object PSObject -Property @{ Region = 'us-west-1'; ZoneName = @('us-west-1a', 'us-west-1b') }
    New-Object PSObject -Property @{ Region = 'us-west-2'; ZoneName = @('us-west-2a', 'us-west-2b', 'us-west-2c') }
)

InModuleScope $ModuleName {

    Describe 'Test-IsFileSystemPath' {

        It 'Should return true for an absolute Unix path' {

            Test-IsFileSystemPath -PropertyValue '/tmp/path' | Should -BeTrue
        }

        It 'Should return true for an absolute Windows path' {

            Test-IsFileSystemPath -PropertyValue 'C:\Temp\path' | Should -BeTrue
        }

        It 'Should return true for a relative Unix path' {

            Test-IsFileSystemPath -PropertyValue '../tmp/path' | Should -BeTrue
        }

        It 'Should return true for a relative Windows path' {

            Test-IsFileSystemPath -PropertyValue '..\Temp\path' | Should -BeTrue
        }

        It 'Should return false for an https uri' {

            Test-IsFileSystemPath -PropertyValue 'https://bucket.s3.amaxonaws.com/prefix' | Should -BeFalse            }

        It 'Should return false for a s3 uri' {

            Test-IsFileSystemPath -PropertyValue 's3://bucket/prefix' | Should -BeFalse
        }

        It 'Should return false for an object that looks like inline lambda code' {

            $code = New-Object PSObject -Property @{
                ZipFile = New-Object PSObject -Property @{
                    "Fn::Join" = @(
                        "`n",
                        @(
                            "import os"
                            "import json"
                        )
                    )
                }
            }

            Test-IsFileSystemPath -ProperyValue $code | Should -BeFalse
        }
    }

    Describe 'New-StackOperationArguments' {

        $templateContentHash = (Get-Content -Raw -Path $global:templatePathJson).GetHashCode()

        Context 'Provides -TemplateBody for local file' {

            $arguments = New-StackOperationArguments -StackName 'pester' -TemplateLocation $global:templatePathJson -CredentialArguments @{}

            It 'Stack name should be "pester"' {

                $arguments['StackName'] | Should Be 'pester'
            }

            It 'TemplateBody should be expected template' {

                $arguments['TemplateBody'].GetHashCode() | Should Be $templateContentHash
            }

            It 'TemplateURL should be undefined' {

                $arguments['TemplateURL'] | Should BeNullOrEmpty
            }
        }

        Context 'Provides -TemplateURL for web URI' {

            $url = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
            $arguments = New-StackOperationArguments -StackName 'pester' -TemplateLocation $url

            It 'Stack name should be "pester"' {

                $arguments['StackName'] | Should Be 'pester'
            }

            It 'TemplateBody should be undefined' {

                $arguments['TemplateBody'] | Should BeNullOrEmpty
            }

            It 'TemplateURL should be given URL' {

                $arguments['TemplateURL'] | Should Be $url
            }
        }

        Context 'Provides normalised -TemplateURL for S3 URI in known region' {

            Mock -CommandName Get-CurrentRegion -MockWith {

                return 'us-east-1'
            }

            $uri = 's3://bucket/path/to/test-stack.json'
            $generatedUrl = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
            $arguments = New-StackOperationArguments -StackName 'pester' -TemplateLocation $uri

            It 'Has called Get-CurrentRegion' {

                Assert-MockCalled -CommandName Get-CurrentRegion -Times 1 -Scope Context
            }

            It 'Stack name should be "pester"' {

                $arguments['StackName'] | Should Be 'pester'
            }

            It 'TemplateBody should be undefined' {

                $arguments['TemplateBody'] | Should BeNullOrEmpty
            }

            It 'TemplateURL should be correct URL for bucket inferred by s3:// ' {

                $arguments['TemplateURL'] | Should Be $generatedUrl
            }
        }
    }

    Describe 'AWS Parameter Type detection' {

        # Parameter type detection is used in formatting output of Get-StackOutputs -AsParameterBlock

        Mock -CommandName Get-EC2AvailabilityZone -MockWith {

            $global:azs | Where-Object { $_.Region -eq $Region }
        }

        Context 'Resource types' {
            function Get-RandomHexString
            {
                param
                (
                    [int]$Length
                )

                $sb = New-Object System.Text.StringBuilder

                for ($i = 0; $i -lt $Length; ++$i)
                {
                    $sb.AppendFormat('{0:x}', (Get-Random -Minimum 0 -Maximum 15)) | Out-Null
                }

                $sb.ToString()
            }

            @(
                @{
                    Prefix = 'i'
                    Name   = 'instance ids'
                    Type   = 'AWS::EC2::Instance::Id'
                },
                @{
                    Prefix = 'sg'
                    Name   = 'security group ids'
                    Type   = 'AWS::EC2::SecurityGroup::Id'
                },
                @{
                    Prefix = 'ami'
                    Name   = 'image ids'
                    Type   = 'AWS::EC2::Image::Id'
                },
                @{
                    Prefix = 'subnet'
                    Name   = 'subnet ids'
                    Type   = 'AWS::EC2::Subnet::Id'
                },
                @{
                    Prefix = 'vol'
                    Name   = 'volume ids'
                    Type   = 'AWS::EC2::Volume::Id'
                },
                @{
                    Prefix = 'vpc'
                    Name   = 'VPC ids'
                    Type   = 'AWS::EC2::VPC::Id'
                }
            ) |
                ForEach-Object {

                # Do 10 random values for each
                It "Recognises 8 digit $($_.Name)" {

                    $p = $_.Prefix
                    $t = $_.Type

                    0..9 | ForEach-Object {
                        Get-ParameterTypeFromStringValue -Value "$p-$(Get-RandomHexString -Length 8)" | Should Be $t
                    }
                }

                It "Recognises 17 digit $($_.Name)" {

                    $p = $_.Prefix
                    $t = $_.Type

                    0..9 | ForEach-Object {
                        Get-ParameterTypeFromStringValue -Value "$p-$(Get-RandomHexString -Length 17)" | Should Be $t
                    }
                }

                It "Treats invalid $($_.Name) as strings" {

                    Get-ParameterTypeFromStringValue -Value "$($_.Prefix)-1234acbd90123456" | Should Be 'String'
                    Get-ParameterTypeFromStringValue -Value "$($_.Prefix)-1234zacbd91234567" | Should Be 'String'
                }

            }
        }

        Context 'Recognises known AZs' {

            # This test will take a long time, as AZ info is lazy-loaded due to the time it takes to retrieve.

            # At time of writing...
            $knownAzs = @(
                'ap-northeast-1a'
                'ap-northeast-1c'
                'ap-northeast-1d'
                'ap-northeast-2a'
                'ap-northeast-2c'
                'ap-south-1a'
                'ap-south-1b'
                'ap-southeast-1a'
                'ap-southeast-1b'
                'ap-southeast-1c'
                'ap-southeast-2a'
                'ap-southeast-2b'
                'ap-southeast-2c'
                'ca-central-1a'
                'ca-central-1b'
                'eu-central-1a'
                'eu-central-1b'
                'eu-central-1c'
                'eu-west-1a'
                'eu-west-1b'
                'eu-west-1c'
                'eu-west-2a'
                'eu-west-2b'
                'eu-west-2c'
                'eu-west-3a'
                'eu-west-3b'
                'eu-west-3c'
                'sa-east-1a'
                'sa-east-1c'
                'us-east-1a'
                'us-east-1b'
                'us-east-1c'
                'us-east-1d'
                'us-east-1e'
                'us-east-1f'
                'us-east-2a'
                'us-east-2b'
                'us-east-2c'
                'us-west-1a'
                'us-west-1b'
                'us-west-2a'
                'us-west-2b'
                'us-west-2c'
            )

            $knownAzs |
                ForEach-Object {

                $az = $_

                It "Recognises '$az' as AWS::EC2::AvailabilityZone::Name" {

                    Get-ParameterTypeFromStringValue -Value $az | Should Be 'AWS::EC2::AvailabilityZone::Name'
                }
            }
        }

        Context 'Treats invalid/unknown AZs as strings' {

            @(
                'ap-northeast-1y'
                'ap-northeast-1z'
                'ap-northwest-1a'
                'ap-northwest-1b'
            ) |
            ForEach-Object {

                $az = $_

                It "Does not recognise '$az' thus treats it as String" {

                    Get-ParameterTypeFromStringValue -Value $az | Should Be 'String'
                }
            }
        }
    }

    Describe 'CommandLineStackParameters' {

        Mock -CommandName Get-Variable -MockWith {

            @("StackName", "ParameterFile") -icontains $Name
        }

        Context 'Should return dynamic params as stack parameters (2 parameters given)' {

            $commandLineParams = @{
                StackName = 'my-stack'
                Param1 = 'value1'
                Param2 = 'value2'
            }

            $params = Get-CommandLineStackParameters -CallerBoundParameters $commandLineParams

            It 'Should have returned 2 parameters' {

                $params.Length | Should Be 2
            }

            It 'Parameters should be of type [Amazon.CloudFormation.Model.Parameter]' {

                $params[0] | Should BeOfType Amazon.CloudFormation.Model.Parameter
            }

            It 'First parameter should have assigned value' {

                $params | Where-Object { $_.ParameterKey -eq 'Param1' } | Select-Object -ExpandProperty ParameterValue | Should -Be 'value1'
            }

            It 'Second parameter should have assigned value' {

                $params | Where-Object { $_.ParameterKey -eq 'Param2' } | Select-Object -ExpandProperty ParameterValue | Should -Be 'value2'
            }
        }

        Context 'Cmdlet/Stack parameter name conflict' {

            # Tests that a stack template may contain a parameter that is the same as one of the CmdLet's core parameters
            # In this case -RoleArn
            # The only way to resolve a conflict between CmdLet and stack parameter name is to use a parameter file

            $commandLineParams = @{
                StackName = 'my-stack'
                ParameterFile = $global:paramPath
            }

            $params = Get-CommandLineStackParameters -CallerBoundParameters $commandLineParams

            It 'Should accept a parameter that matches one of the CmdLet parameters from parameter file (-RoleArn)' {
                $params | Where-Object { $_.ParameterKey -eq 'RoleARN'} | Select-Object -ExpandProperty ParameterValue | Should -Be 'arn:aws:iam:::matches_a_command_line_argument'
            }
        }

        Context 'Load parameter file (containing 4 parameters)' {

            $commandLineParams = @{
                StackName = 'my-stack'
                ParameterFile = $global:paramPath
            }

            $params = Get-CommandLineStackParameters -CallerBoundParameters $commandLineParams

            It 'Should have returned 4 parameters' {

                $params.Length | Should Be 4
            }

            # Lazy
            1..3 |
            Foreach-Object {

                $paramNo = $_

                It "FileParam$paramNo should be FileValue$paramNo" {

                    $params | Where-Object { $_.ParameterKey -eq "FileParam$paramNo"} | Select-Object -ExpandProperty ParameterValue | Should -Be "FileValue$paramNo"
                }
            }

            It 'RoleARN should be arn:aws:iam:::matches_a_command_line_argument' {

                $params | Where-Object { $_.ParameterKey -eq 'RoleARN'} | Select-Object -ExpandProperty ParameterValue | Should -Be 'arn:aws:iam:::matches_a_command_line_argument'
            }
        }

        Context 'Should override parameter file when same param is on the command line' {

            $commandLineParams = @{
                StackName = 'my-stack'
                ParameterFile = $global:paramPath
                FileParam1 = 'CommandLineValue1'
            }

            $params = Get-CommandLineStackParameters -CallerBoundParameters $commandLineParams

            It 'Should have returned 4 parameters' {

                $params.Length | Should Be 4
            }

            It 'Should have overridden FileParam1 with value CommandLineValue1' {

                $params | Where-Object { $_.ParameterKey -eq 'FileParam1'} | Select-Object -ExpandProperty ParameterValue | Should -Be 'CommandLineValue1'
            }

            # Lazy
            2,3 |
            Foreach-Object {

                $paramNo = $_

                It "FileParam$paramNo should be FileValue$paramNo" {

                    $params | Where-Object { $_.ParameterKey -eq "FileParam$paramNo"} | Select-Object -ExpandProperty ParameterValue | Should -Be "FileValue$paramNo"
                }
            }

            It 'RoleARN should be arn:aws:iam:::matches_a_command_line_argument' {

                $params | Where-Object { $_.ParameterKey -eq 'RoleARN'} | Select-Object -ExpandProperty ParameterValue | Should -Be 'arn:aws:iam:::matches_a_command_line_argument'
            }
        }
    }

    Describe 'Region detection - Get-CurrentRegion' {

        $allRegions = (Get-AWSRegion).Region

        Mock -CommandName Get-EC2Region -MockWith {

            (Get-AWSRegion).Region |
            ForEach-Object {

                New-Object PSObject -Property @{ RegionName = $_ }
            }
        }

        It 'Should throw if default region never initialised' {

            if ($null -eq [Amazon.Runtime.FallbackRegionFactory]::GetRegionEndpoint())
            {
                { Get-CurrentRegion -CredentialArguments @{} } | Should -Throw
            }
            else
            {
                Set-ItResult -Inconclusive -Because "You have already initialised a region (should not be the case on AppVeyor)."
            }
        }

        Context 'Region passed as command line argument' {

            $allRegions | Foreach-Object {

                $r = $_

                It "Should retun '$r' when given as argument" {

                    $credArgs = @{ Region = $r}

                    Get-CurrentRegion -CredentialArguments $credArgs | Should -Be $r
                }
            }
        }

        Context 'Region set by Set-DefaultAWSRegion' {

            BeforeEach {

                if (Test-Path -Path variable:StoredAWSRegion)
                {
                    Remove-Item  -Path variable:StoredAWSRegion
                }
            }

            $allRegions | Foreach-Object {

                $r = $_

                It "Should return '$r' set by Set-DefaultAWSRegion if no specific region passed" {

                    Set-DefaultAWSRegion -Region $r
                    Get-CurrentRegion -CredentialArguments $credArgs | Should -Be $r
                }
            }
        }
    }

    Describe 'Template Backup' {

        Mock -CommandName Get-CFNTemplate -MockWith {

            Get-Content -Raw -Path $global:templatePathJson
        }

        Mock -CommandName Get-CFNStackResourceList -MockWith {}

        Context 'Stack without parameters' {

            Mock -CommandName Get-CFNStack -MockWith {

                New-Object PSObject -Property @{

                    StackName = 'test-stack'
                    StackId = 'test-stack'
                    Parameters = @()
                }
            }

            $backupPathWithoutExtension = Join-Path $TestDrive "test-stack"
            Save-TemplateBackup -StackName test-backup -OutputPath $TestDrive

            It 'Should have called Get-CFNStack' {

                Assert-MockCalled -CommandName Get-CFNStack -Times 1 -Scope Context
            }

            It 'Should have called Get-CFNTemplate' {

                Assert-MockCalled -CommandName Get-CFNTemplate -Times 1 -Scope Context
            }

            It 'Should have called Get-CFNStackResourceList' {

                Assert-MockCalled -CommandName Get-CFNStackResourceList -Times 1 -Scope Context
            }

            It 'Should create a template backup at given output path' {

                Test-Path -Path "$($backupPathWithoutExtension).template.bak.json" | Should -BeTrue
            }

            It 'Should not create a parameter file at given output path' {

                Test-Path -Path "$($backupPathWithoutExtension).parameters.bak.json" | Should -BeFalse
            }
        }

        Context 'Stack with parameters' {

            Mock -CommandName Get-CFNStack -MockWith {

                New-Object PSObject -Property @{

                    StackName = 'test-stack'
                    StackId = 'test-stack'
                    Parameters = @(
                        @{
                            ParameterKey = 'VpcCidr'
                            ParameterValue = '10.0.0.0/16'
                        }
                        @{
                            ParameterKey = 'DnsSupport'
                            ParameterValue = 'true'
                        }
                    )
                }
            }

            $backupPathWithoutExtension = Join-Path $TestDrive "test-stack"
            Save-TemplateBackup -StackName test-backup -OutputPath $TestDrive

            It 'Should have called  Get-CFNStack' {

                Assert-MockCalled -CommandName Get-CFNStack -Times 1 -Scope Context
            }

            It 'Should have called  Get-CFNTemplate' {

                Assert-MockCalled -CommandName Get-CFNTemplate -Times 1 -Scope Context
            }

            It 'Should have called  Get-CFNStackResourceList' {

                Assert-MockCalled -CommandName Get-CFNStackResourceList -Times 1 -Scope Context
            }

            It 'Should create a template backup at given output path' {

                Test-Path -Path "$($backupPathWithoutExtension).template.bak.json" | Should -BeTrue
            }

            It 'Should create a parameter file at given output path' {

                Test-Path -Path "$($backupPathWithoutExtension).parameters.bak.json" | Should -BeTrue
            }
        }
    }

    Describe 'S3 Interaction' {

        $regionList = (Get-AWSRegion).Region

        Mock -CommandName Get-EC2Region -MockWith {

            (Get-AWSRegion).Region |
            ForEach-Object {

                New-Object PSObject -Property @{ RegionName = $_ }
            }
        }

        Mock -CommandName Get-STSCallerIdentity -MockWith {
            New-Object PSObject -Property @{
                Account = '000000000000'
            }
        }

        Mock -Command Get-S3BucketTagging -MockWith {}

        Mock -Command Write-S3BucketTagging -MockWith {}

        $regionList |
        ForEach-Object {

            $region = $_

            Context "Create S3 Cloudformation Bucket: $region" {

                Mock -CommandName Get-S3Bucket -MockWith {}

                Mock -CommandName New-S3Bucket -MockWith { $true }

                Mock -CommandName Get-S3BucketLocation -MockWith {

                    New-Object PSObject -Property @{
                        Value = $region
                    }
                }

                $expectedBucketName = "cf-templates-pscloudformation-$($region)-000000000000"
                $expectedBucketUrl = [uri]"https://s3.$($region).amazonaws.com/$expectedBucketName"
                $result = Get-CloudFormationBucket -CredentialArguments @{ Region = $region }

                It 'Should create the bucket' {

                    # TODO check with MockS3
                    $true | Should -BeTrue
                }

                It 'Should have expected name' {

                    $result.BucketName | Should Be $expectedBucketName
                }

                It 'Should have expected URL' {

                    $result.BucketUrl | Should Be $expectedBucketUrl
                }

                It 'Should be tagged' {

                    Assert-MockCalled -CommandName Write-S3BucketTagging -Times 1 -Scope COntext
                }
            }
        }

        $regionList |
        Foreach-Object {

            $region = $_

            Context "Get existing bucket: $region" {

                Mock -CommandName Get-S3Bucket -MockWith { $true }

                Mock -CommandName Get-S3BucketLocation -MockWith {

                    New-Object PSObject -Property @{
                        Value = $region
                    }
                }

                $expectedBucketName = "cf-templates-pscloudformation-$($region)-000000000000"
                $expectedBucketUrl = [uri]"https://s3.$($region).amazonaws.com/$expectedBucketName"

                $result = Get-CloudFormationBucket -CredentialArguments @{ Region = $region }

                It "Should return expected bucket name" {

                    $result.BucketName | Should Be $expectedBucketName
                }

                It 'Should return expected bucket URL' {

                    $result.BucketUrl | Should Be $expectedBucketUrl
                }
            }
        }

        $regionList |
        Foreach-Object {

            $region = $_

            Context "Tag untagged bucket: $region" {

                Mock -CommandName Get-S3Bucket -MockWith { $true }

                Mock -CommandName Get-S3BucketLocation -MockWith {

                    New-Object PSObject -Property @{
                        Value = $region
                    }
                }

                Get-CloudFormationBucket -CredentialArguments @{ Region = $region } | Out-Null

                It 'Has tagged the bucket' {

                    Assert-MockCalled -CommandName Write-S3BucketTagging -Times 1 -Scope Context
                }
            }
        }

        Context 'Should not copy template to S3 if size less than 51200' {

            Mock -CommandName Get-S3Bucket -MockWith {}

            $template = New-Object String -ArgumentList '*', 51119
            $tempTemplatePath = [IO.Path]::Combine($TestDrive, "not-oversize.json")
            [IO.File]::WriteAllText($tempTemplatePath, $template, (New-Object System.Text.ASCIIEncoding))

            $stackArguments = @{
                TemplateBody = $template
            }

            Copy-OversizeTemplateToS3 -TemplateLocation $tempTemplatePath -CredentialArguments @{} -StackArguments $stackArguments

            It 'Should not have called Get-S3Bucket' {

                Assert-MockCalled -CommandName Get-S3Bucket -Times 0
            }

            It 'Arguments for *-CFNStack should contain TemplateBody' {

                $stackArguments.ContainsKey('TemplateBody') | Should -BeTrue
            }

            It 'Arguments for *-CFNStack should not contain TemplateURL' {

                $stackArguments.ContainsKey('TemplateURL') | Should -BeFalse
            }
        }

        $regionList |
        ForEach-Object {

            $region = $_

            Context "Should copy oversize template (>= 51200 bytes) to S3: $region" {

                $template = New-Object String -ArgumentList '*', 51200
                $tempTemplatePath = [IO.Path]::Combine($TestDrive, "oversize.json")
                [IO.File]::WriteAllText($tempTemplatePath, $template, (New-Object System.Text.ASCIIEncoding))

                $stackArguments = @{
                    StackName = "oversize-stack"
                    TemplateBody = $template
                }

                Mock -CommandName Get-CloudFormationBucket -MockWith {
                    New-Object PSObject -Property @{
                        BucketName = 'test-bucket'
                        BucketUrl = [uri]"https://s3.$region.amazonaws.com/test-bucket"
                    }
                }

                Mock -CommandName Get-Date -MockWith {

                    New-Object DateTime -ArgumentList (2000,1,1,0,0,0,0)
                }

                Mock -CommandName Resolve-Path -MockWith {

                    New-Object PSObject -Property @{
                        Path = Join-Path ([IO.Path]::GetTempPath()) 'test.json'
                    }
                }

                Mock -CommandName Get-S3BucketLocation -MockWith {

                    New-Object PSObject -Property @{
                        Value = $region
                    }
                }

                Mock -CommandName Write-S3Object -MockWith { }

                $expectedUrl = "https://s3.$region.amazonaws.com/test-bucket/20000101000000000_oversize-stack_oversize.json"

                Copy-OversizeTemplateToS3 -TemplateLocation $tempTemplatePath -CredentialArguments @{ Region = $region } -StackArguments $stackArguments

                It 'Should have called Write-S3Object' {

                    Assert-MockCalled -CommandName Write-S3Object -Exactly -Times 1 -Scope Context
                }

                It 'Arguments for *-CFNStack should not contain TemplateBody' {

                    $stackArguments.ContainsKey('TemplateBody') | Should -BeFalse
                }

                It 'Arguments for *-CFNStack should contain TemplateURL' {

                    $stackArguments.ContainsKey('TemplateURL') | Should -BeTrue
                }

                It 'TemplateURL should be expected URL' {

                    $stackArguments['TemplateURL'] | Should Be $expectedUrl
                }
            }
        }
    }

    Describe 'Template Manipulation' {

        $templateContentHash = (Get-Content -Raw -Path $global:templatePathJson).GetHashCode()

        Context 'Creating File Template Resolver' {

            $resolver = New-TemplateResolver -TemplateLocation $global:templatePathJson

            It 'Resolver type should be File' {

                $resolver.Type | Should Be 'File'
            }

            It 'Should have loaded the correct template' {

                ($resolver.ReadTemplate()).GetHashCode() | Should Be $templateContentHash
            }
        }

        Context 'Creating URL Template Resolver' {

            Mock -CommandName Read-S3Object -MockWith {

                Copy-Item $global:templatePathJson $File
            }

            $url = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
            $resolver = New-TemplateResolver -TemplateLocation $url

            It 'Resolver type should be Url' {

                $resolver.Type | Should Be 'Url'
            }

            It 'Resolver URL should be expected URL' {

                $resolver.Url | Should Be $url
            }

            It 'Resolver S3 bucket should be expected value' {

                $resolver.BucketName | Should Be 'bucket'
            }

            It 'Resolver S3 key should be expected value' {

                $resolver.Key | Should Be 'path/to/test-stack.json'
            }

            It 'Should have downloaded the correct template' {

                ($resolver.ReadTemplate()).GetHashCode() | Should Be $templateContentHash
            }

            It 'Should have called Get-S3Object' {

                # Call is made by ReadTemplate()
                Assert-MockCalled -CommandName Read-S3Object -Times 1 -Scope Context
            }
        }

        Context 'Creating URL resolver from s3://... when AWS region is set in the session context' {

            Mock -CommandName Get-CurrentRegion -MockWith {

                return 'us-east-1'
            }

            $uri = 's3://bucket/path/to/test-stack.json'
            $generatedUrl = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'

            $resolver = New-TemplateResolver -TemplateLocation $uri

            It 'Has called Get-CurentRegion' {

                Assert-MockCalled -CommandName Get-CurrentRegion -Times 1 -Scope Context
            }

            It 'Resolver type should be Url' {

                $resolver.Type | Should Be 'Url'
            }

            It 'Resolver URL should be expected URL' {

                $resolver.Url | Should Be $generatedUrl
            }


            It 'Resolver S3 bucket should be expected value' {

                $resolver.BucketName | Should Be 'bucket'
            }

            It 'Resolver S3 key should be expected value' {

                $resolver.Key | Should Be 'path/to/test-stack.json'
            }
        }

        Context 'Creating URL resolver from s3://... when AWS region is set on the command line' {

            $uri = 's3://bucket/path/to/test-stack.json'
            $generatedUrl = 'https://s3-us-west-1.amazonaws.com/bucket/path/to/test-stack.json'

            $resolver = New-TemplateResolver -TemplateLocation $uri -CredentialArguments @{ Region = 'us-west-1' }

            It 'Resolver type should be Url' {

                $resolver.Type | Should Be 'Url'
            }

            It 'Resolver URL should be expected URL' {

                $resolver.Url | Should Be $generatedUrl
            }


            It 'Resolver S3 bucket should be expected value' {

                $resolver.BucketName | Should Be 'bucket'
            }

            It 'Resolver S3 key should be expected value' {

                $resolver.Key | Should Be 'path/to/test-stack.json'
            }
        }

        Context 'Creating URL resolver from s3://... when no AWS region can be determined' {

            Mock -CommandName Get-CurrentRegion -MockWith {

                throw "Cannot determine AWS Region. Either use Set-DefaultAWSRegion to set in shell, or supply -Region parameter."
            }


            $uri = 's3://bucket/path/to/test-stack.json'

            It 'Should throw because bucket region cannot be detemined' {

                { New-TemplateResolver -TemplateLocation $uri } | Should -Throw
            }

            It 'Has called Get-CurentRegion' {

                Assert-MockCalled -CommandName Get-CurrentRegion -Times 1 -Scope Context
            }
        }

        Context 'Resolver with -UsePreviousTemplate' {

            Mock -CommandName Get-CFNTemplate -MockWith {

                Get-Content -Raw $global:templatePathJson
            }

            $resolver = New-TemplateResolver -StackName test-stack -UsePreviousTemplate $true

            It 'Resolver type should be UsePreviousTemplate' {

                $resolver.Type | Should Be 'UsePreviousTemplate'
            }

            It 'Should download correct template from exitsing stack' {

                ($resolver.ReadTemplate()).GetHashCode() | Should Be $templateContentHash
            }

            It 'Should have called Get-CFNTemplate' {

                # Called by ReadTemplate() above
                Assert-MockCalled Get-CFNTemplate -Times 1 -Scope Context
            }
        }

        ('Json', 'Yaml') |
        Foreach-Object {

            $format = $_

            Context "Stack Parameter Parsing - $format" {

                $location = Get-Variable -Name "templatePath$format" -ValueOnly

                $resolver = New-TemplateResolver -TemplateLocation $location
                $parameters = Get-TemplateParameters -TemplateResolver $resolver

                It 'Should have read two parameters from the input' {

                    $parameters.PSObject.Properties | SHould -HaveCount 2
                }

                ('VpcCidr', 'DnsSupport') |
                Foreach-Object {

                    $paramName = $_

                    It "Should have a parameter '$paramName'" {

                        $parameters.PSObject.Properties.Name | Where-Object { $_ -ceq $paramName } | Should -HaveCount 1
                    }

                    It "Type of '$paramName' should be String" {

                        $parameters.$paramName.Type | Should -Be 'String'
                    }
                }

                It 'VpcCidr should have the correct value for AllowedPattern' {

                    $pattern = '^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(/([0-9]|[1-2][0-9]|3[0-2]))$'
                    $parameters.VpcCidr.AllowedPattern | Should -Be $pattern
                }

                ('true', 'false') |
                Foreach-Object {

                    $value = $_

                    It "DnsSupport should have '$value' in AllowedValues" {

                        $parameters.DnsSupport.AllowedValues | Should -Contain $value
                    }
                }
            }
        }
    }
}