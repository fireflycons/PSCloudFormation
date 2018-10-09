$ModuleName = 'PSCloudFormation'

# http://www.lazywinadmin.com/2016/05/using-pester-to-test-your-manifest-file.html
# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = "$(Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition))\$ModuleName\$ModuleName.psd1"

Import-Module -Name $ManifestFile

$global:templatePath = Join-Path $PSScriptRoot test-stack.json
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

InModuleScope 'PSCloudFormation' {

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

                Mock -CommandName Get-DefaultAWSRegion -MockWith {

                    return @{
                        Region = 'us-east-1'
                    }
                }

                $uri = 's3://bucket/path/to/test-stack.json'
                $generatedUrl = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'

                $resolver = New-TemplateResolver -TemplateLocation $uri

                $resolver.Type | Should Be 'Url'
                $resolver.Url | Should Be $generatedUrl
                $resolver.BucketName | Should Be 'bucket'
                $resolver.Key | Should Be 'path/to/test-stack.json'
            }

            It 'Creates UsePreviousTemplate resolver when given a stack name instead of a template location' {

                $resolver = New-TemplateResolver -StackName test-stack
                $resolver.Type | Should Be 'UsePreviousTemplate'
            }

            It 'Throws with s3 URI when default AWS region cannot be determined' {

                Mock -CommandName Get-DefaultAWSRegion -MockWith {}

                { New-TemplateResolver -TemplateLocation 's3://bucket/path/to/test-stack.json' } | Should Throw
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
            }

            It 'UsePreviousTemplate resolver returns original template' {

                Mock -CommandName Get-CFNTemplate -MockWith {

                    Get-Content -Raw $global:templatePath
                }
    
                    $resolver = New-TemplateResolver -StackName test-stack
                ($resolver.ReadTemplate()).GetHashCode() | Should Be $templateContentHash
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

                Mock -CommandName Get-DefaultAWSRegion -MockWith {

                    return @{
                        Region = 'us-east-1'
                    }
                }

                $uri = 's3://bucket/path/to/test-stack.json'
                $generatedUrl = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
                $args = New-StackOperationArguments -StackName 'pester' -TemplateLocation $uri
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
    }
}