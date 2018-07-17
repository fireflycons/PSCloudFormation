$ModuleName = 'PSCloudFormation'

# http://www.lazywinadmin.com/2016/05/using-pester-to-test-your-manifest-file.html
# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = "$(Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition))\$ModuleName\$ModuleName.psd1"

Import-Module -Name $ManifestFile

$global:templatePath = Join-Path $PSScriptRoot test-stack.json

InModuleScope 'PSCloudFormation' {

    Describe 'PSCloudFormation - Private' {

        $templateContentHash = (Get-Content -Raw -Path $global:templatePath).GetHashCode()

        Context 'New-TemplateResolver' {

            It 'Creates a file resolver for local file' {

                $resolver = New-TemplateResolver -TemplateLocation $global:templatePath

                $resolver.IsFile | Should Be $true
            }

            It 'Creates a URL resolver for web URI' {

                $url = 'https://s3-us-east-1.amazonaws.com/bucket/path/to/test-stack.json'
                $resolver = New-TemplateResolver -TemplateLocation $url

                $resolver.IsFile | Should Be $false
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

                $resolver.IsFile | Should Be $false
                $resolver.Url | Should Be $generatedUrl
                $resolver.BucketName | Should Be 'bucket'
                $resolver.Key | Should Be 'path/to/test-stack.json'
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

            @(
                @{
                    Prefix = 'i'
                    Name = 'instance id'
                    Type = 'AWS::EC2::Instance::Id'
                },
                @{
                    Prefix = 'sg'
                    Name = 'security group id'
                    Type = 'AWS::EC2::SecurityGroup::Id'
                },
                @{
                    Prefix = 'ami'
                    Name = 'image id'
                    Type = 'AWS::EC2::Image::Id'
                },
                @{
                    Prefix = 'subnet'
                    Name = 'subnet id'
                    Type = 'AWS::EC2::Subnet::Id'
                },
                @{
                    Prefix = 'vol'
                    Name = 'volumne id'
                    Type = 'AWS::EC2::Volume::Id'
                },
                @{
                    Prefix = 'vpc'
                    Name = 'VPC id'
                    Type = 'AWS::EC2::VPC::Id'
                }
            ) |
            ForEach-Object {

                It "Recognises 8 digit $($_.Name)" {

                    Get-ParameterTypeFromStringValue -Value "$($_.Prefix)-1234acbd" | Should Be $_.Type
                }

                It "Recognises 17 digit $($_.Name)" {

                    Get-ParameterTypeFromStringValue -Value "$($_.Prefix)-1234acbd901234567" | Should Be $_.Type
                }

                It "Thinks an invalid $($_.Name) is a string" {

                    Get-ParameterTypeFromStringValue -Value "$($_.Prefix)-1234acbd90123456" | Should Be 'String'
                    Get-ParameterTypeFromStringValue -Value "$($_.Prefix)-1234zacbd91234567" | Should Be 'String'
                }
            }
        }
    }
}