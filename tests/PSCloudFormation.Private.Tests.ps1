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

$global:templatePath = Join-Path $PSScriptRoot test-stack.json
$global:paramPath = Join-Path $PSScriptRoot test-params.json
$global:haveYaml = $null -ne (Get-Module -ListAvailable | Where-Object {  $_.Name -ieq 'powershell-yaml' })

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

    Describe 'PSCloudFormation - Private' {

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

        $templateContentHash = (Get-Content -Raw -Path $global:templatePath).GetHashCode()

        Context 'New-TemplateResolver' {

            It 'Creates a file resolver for local file' {

                $resolver = New-TemplateResolver -TemplateLocation $global:templatePath

                $resolver.Type | Should Be 'File'
            }

            It 'Creates a URL resolver for web URI' {

                $url = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
                $resolver = New-TemplateResolver -TemplateLocation $url

                $resolver.Type | Should Be 'Url'
                $resolver.Url | Should Be $url
                $resolver.BucketName | Should Be 'bucket'
                $resolver.Key | Should Be 'path/to/test-stack.json'
            }

            It 'Creates URL resolver for s3 URI when region is known' {

                Mock -CommandName Get-CurrentRegion -MockWith {

                    return 'us-east-1'
                }

                $uri = 's3://bucket/path/to/test-stack.json'
                $generatedUrl = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'

                $resolver = New-TemplateResolver -TemplateLocation $uri

                Assert-MockCalled -CommandName Get-CurrentRegion -Times 1
                $resolver.Type | Should Be 'Url'
                $resolver.Url | Should Be $generatedUrl
                $resolver.BucketName | Should Be 'bucket'
                $resolver.Key | Should Be 'path/to/test-stack.json'
            }

            It 'Creates UsePreviousTemplate resolver when -UsePreviousTemplate set.' {

                $resolver = New-TemplateResolver -StackName test-stack -UsePreviousTemplate $true
                $resolver.Type | Should Be 'UsePreviousTemplate'
            }

            It 'File resolver returns local file' {

                $resolver = New-TemplateResolver -TemplateLocation $global:templatePath
                ($resolver.ReadTemplate()).GetHashCode() | Should Be $templateContentHash
            }

            It 'URL resolver returns correct file' {

                Mock -CommandName Read-S3Object -MockWith {

                    Copy-Item $global:templatePath $File
                }

                $resolver = New-TemplateResolver -TemplateLocation 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
                ($resolver.ReadTemplate()).GetHashCode() | Should Be $templateContentHash
                Assert-MockCalled -CommandName Read-S3Object -Times 1
            }

            It 'UsePreviousTemplate resolver returns original template' {

                Mock -CommandName Get-CFNTemplate -MockWith {

                    Get-Content -Raw $global:templatePath
                }

                $resolver = New-TemplateResolver -StackName test-stack -UsePreviousTemplate $true
                ($resolver.ReadTemplate()).GetHashCode() | Should Be $templateContentHash
                Assert-MockCalled -CommandName Get-CFNTemplate -Times 1
            }
        }

        Context 'New-StackOperationArguments' {

            # Creates an argument hash of parameters needed by New-CFNStack and Update-CFNStack

            It 'Provides -TemplateBody for local file' {

                $args = New-StackOperationArguments -StackName 'pester' -TemplateLocation $global:templatePath
                $args['StackName'] | Should Be 'pester'
                $args['TemplateBody'].GetHashCode() | Should Be $templateContentHash
                $args['TemplateURL'] | Should BeNullOrEmpty
            }

            It 'Provides -TemplateURL for web URI' {

                $url = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
                $args = New-StackOperationArguments -StackName 'pester' -TemplateLocation $url

                $args['StackName'] | Should Be 'pester'
                $args['TemplateURL'] | Should Be $url
                $args['TemplateBody'] | Should BeNullOrEmpty
            }

            It 'Provides normalised -TemplateURL for s3 URI in known region' {

                Mock -CommandName Get-CurrentRegion -MockWith {

                    return 'us-east-1'
                }


                $uri = 's3://bucket/path/to/test-stack.json'
                $generatedUrl = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
                $args = New-StackOperationArguments -StackName 'pester' -TemplateLocation $uri

                Assert-MockCalled -CommandName Get-CurrentRegion -Times 1
                $args['StackName'] | Should Be 'pester'
                $args['TemplateURL'] | Should Be $generatedUrl
                $args['TemplateBody'] | Should BeNullOrEmpty
            }
        }

        Context 'AWS Parameter Type detection' {

            # Parameter type detection is used in formatting output of Get-StackOutputs -AsParameterBlock

            Mock -CommandName Get-EC2AvailabilityZone -MockWith {

                $global:azs | Where-Object { $_.Region -eq $Region }
            }

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

            It 'Recognises known AZs' {

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

                    Get-ParameterTypeFromStringValue -Value $_ | Should Be 'AWS::EC2::AvailabilityZone::Name'
                }
            }

            It 'Treats invalid AZs as strings' {

                @(
                    'ap-northeast-1y'
                    'ap-northeast-1z'
                    'ap-northwest-1a'
                    'ap-northwest-1b'
                ) |
                    ForEach-Object {

                    Get-ParameterTypeFromStringValue -Value $_ | Should Be 'String'
                }
            }
        }

        Context 'CommandLineStackParameters' {

            Mock -CommandName Get-Variable -MockWith {

                @("StackName", "ParameterFile") -icontains $Name
            }

            It 'Should return dynamic params as stack parameters' {

                $commandLineParams = @{
                    StackName = 'my-stack'
                    Param1 = 'value1'
                    Param2 = 'Value2'
                }

                $params = Get-CommandLineStackParameters -CallerBoundParameters $commandLineParams

                $params.Length | Should Be 2
            }

            It 'Should load parameter file' {

                $commandLineParams = @{
                    StackName = 'my-stack'
                    ParameterFile = $global:paramPath
                }

                $params = Get-CommandLineStackParameters -CallerBoundParameters $commandLineParams

                $params.Length | Should Be 4
                $params | Where-Object { $_.ParameterKey -eq 'FileParam1'} | Select-Object -ExpandProperty ParameterValue | Should -Be 'FileValue1'
            }

            It 'Should override parameter file when same param is on the command line' {

                $commandLineParams = @{
                    StackName = 'my-stack'
                    ParameterFile = $global:paramPath
                    FileParam1 = 'CommandLineValue1'
                }

                $params = Get-CommandLineStackParameters -CallerBoundParameters $commandLineParams

                $params.Length | Should Be 4
                $params | Where-Object { $_.ParameterKey -eq 'FileParam1'} | Select-Object -ExpandProperty ParameterValue | Should -Be 'CommandLineValue1'
                $params | Where-Object { $_.ParameterKey -eq 'FileParam2'} | Select-Object -ExpandProperty ParameterValue | Should -Be 'FileValue2'

            }

            It 'Should accept a parameter that matches a defined command line argument from parameter file' {

                $commandLineParams = @{
                    StackName = 'my-stack'
                    ParameterFile = $global:paramPath
                }

                $params = Get-CommandLineStackParameters -CallerBoundParameters $commandLineParams

                $params | Where-Object { $_.ParameterKey -eq 'RoleARN'} | Select-Object -ExpandProperty ParameterValue | Should -Be 'arn:aws:iam:::matches_a_command_line_argument'

            }
        }

        Context 'Get-CurrentRegion' {

            BeforeEach {

                if (Test-Path -Path variable:StoredAWSRegion)
                {
                    Remove-Item  -Path variable:StoredAWSRegion
                }
            }

            It 'Should return region passed in credential arguments' {

                $credArgs = @{ Region = 'eu-west-2'}

                Get-CurrentRegion -CredentialArguments $credArgs | Should -Be 'eu-west-2'
            }

            It 'Should throw if default region never initialised' {

                if ($null -eq [Amazon.Runtime.FallbackRegionFactory]::GetRegionEndpoint())
                {
                    { Get-CurrentRegion -CredentialArguments @{} } | Should -Throw
                }
                else
                {
                    Set-ItResult -Inconclusive -Because "you have already initialised a region (should not be the case on AppVeyor)."
                }
            }

            It 'Should return region set by Set-DefaultAWSRegion if no specific region passed' {

                Set-DefaultAWSRegion -Region us-east-2
                Get-CurrentRegion -CredentialArguments $credArgs | Should -Be 'us-east-2'
            }
        }


        Context 'S3 Cloudformation Bucket' {

            Mock Get-STSCallerIdentity -MockWith {
                New-Object PSObject -Property @{
                    Account = '0123456789012'
                }
            }

            It 'Should return bucket details if bucket exists' {

                $region = 'us-east-1'
                $expectedBucketName = "cf-templates-pscloudformation-$($region)-000000000000"
                $expectedBucketUrl = [uri]"https://s3.$($region).amazonaws.com/$expectedBucketName"

                Mock -CommandName Get-STSCallerIdentity -MockWith {

                    New-Object PSObject -Property @{
                        Account = '000000000000'
                    }
                }

                Mock -CommandName Get-S3BucketLocation -MockWith {

                    New-Object PSObject -Property @{
                        Value = $region
                    }
                }

                $result = Get-CloudFormationBucket -CredentialArguments @{ Region = $region }
                Assert-MockCalled -CommandName Get-STSCallerIdentity -Times 1
                $result.BucketName | Should Be $expectedBucketName
                $result.BucketUrl | Should Be $expectedBucketUrl
            }

            It 'Should create bucket if bucket does not exist' {

                $region = 'us-east-1'
                $expectedBucketName = "cf-templates-pscloudformation-$($region)-000000000000"
                $expectedBucketUrl = [uri]"https://s3.$($region).amazonaws.com/$expectedBucketName"
                $script:callCount = 0

                Mock -CommandName Get-STSCallerIdentity -MockWith {

                    New-Object PSObject -Property @{
                        Account = '000000000000'
                    }
                }

                Mock -CommandName Get-S3BucketLocation -MockWith {

                    if ($script:callCount++ -eq 0)
                    {
                        throw "The specified bucket does not exist"
                    }

                    New-Object PSObject -Property @{
                        Value = $region
                    }
                }

                Mock -CommandName New-S3Bucket -MockWith {
                    $true
                }

                $result = Get-CloudFormationBucket -CredentialArguments @{ Region = $region }
                $result.BucketName | Should Be $expectedBucketName
                $result.BucketUrl | Should Be $expectedBucketUrl

                Assert-MockCalled -CommandName Get-S3BucketLocation -Times 2
                Assert-MockCalled -CommandName New-S3Bucket -Times 1
            }

            It 'Should not copy template to S3 if size less than 51200' {

                $template = New-Object String -ArgumentList '*', 51119
                $templatePath = [IO.Path]::Combine($TestDrive, "not-oversize.json")
                [IO.File]::WriteAllText($templatePath, $template, (New-Object System.Text.ASCIIEncoding))

                $stackArguments = @{
                    TemplateBody = $template
                }

                Mock -CommandName Get-CloudFormationBucket -MockWith { }

                Mock -CommandName Resolve-Path -MockWith { }

                Mock -CommandName Write-S3Object -MockWith { }

                $dateStamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
                Copy-OversizeTemplateToS3 -TemplateLocation $templatePath -CredentialArguments @{} -StackArguments $stackArguments

                $stackArguments.ContainsKey('TemplateBody') | Should Be $true
                $StackArguments.ContainsKey('TemplateURL') | Should Be $false

                Assert-MockCalled -CommandName Get-CloudFormationBucket -Times 0
                Assert-MockCalled -CommandName Resolve-Path -Times 0
                Assert-MockCalled -CommandName Write-S3Object -Times 0
            }

            It 'Should copy oversize template to S3' {

                $template = New-Object String -ArgumentList '*', 51200
                $templatePath = [IO.Path]::Combine($TestDrive, "oversize.json")
                [IO.File]::WriteAllText($templatePath, $template, (New-Object System.Text.ASCIIEncoding))

                $stackArguments = @{
                    TemplateBody = $template
                }

                Mock -CommandName Get-CloudFormationBucket -MockWith {
                    New-Object PSObject -Property @{
                        BucketName = 'test-bucket'
                        BucketUrl = [uri]'https://s3.us-east-1.amazonaws.com/test-bucket'
                    }
                }

                Mock -CommandName Resolve-Path -MockWith {

                    New-Object PSObject -Property @{
                        Path = Join-Path ([IO.Path]::GetTempPath()) 'test.json'
                    }
                }

                Mock -CommandName Write-S3Object -MockWith { }

                $dateStamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
                Copy-OversizeTemplateToS3 -TemplateLocation $templatePath -CredentialArguments @{} -StackArguments $stackArguments

                $stackArguments.ContainsKey('TemplateBody') | Should Be $false
                $StackArguments.ContainsKey('TemplateURL') | Should Be $true
                $stackArguments['TemplateURL'] | Should Be "https://s3.us-east-1.amazonaws.com/test-bucket/$($dateStamp)-oversize.json"
            }
        }
    }
}