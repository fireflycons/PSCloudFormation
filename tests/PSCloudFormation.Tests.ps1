$ModuleName = 'PSCloudFormation'

# http://www.lazywinadmin.com/2016/05/using-pester-to-test-your-manifest-file.html
# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = "$(Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition))\$ModuleName\$ModuleName.psd1"

$TestStackArn = 'arn:aws:cloudformation:eu-west-1:000000000000:stack/pester/00000000-0000-0000-0000-000000000000'

# Import the module and store the information about the module
$ModuleInformation = Import-Module -Name $ManifestFile -PassThru

Describe "$ModuleName Module - Testing Manifest File (.psd1)"{
    Context 'Manifest'{
        It 'Should contain RootModule'{
            $ModuleInformation.RootModule | Should not BeNullOrEmpty
        }
        It 'Should contain Author'{
            $ModuleInformation.Author | Should not BeNullOrEmpty
        }
        It 'Should contain Company Name'{
            $ModuleInformation.CompanyName | Should not BeNullOrEmpty
        }
        It 'Should contain Description'{
            $ModuleInformation.Description | Should not BeNullOrEmpty
        }
        It 'Should contain Copyright'{
            $ModuleInformation.Copyright | Should not BeNullOrEmpty
        }
        It 'Should contain License'{
            $ModuleInformation.LicenseURI | Should not BeNullOrEmpty
        }
        It 'Should contain a Project Link'{
            $ModuleInformation.ProjectURI | Should not BeNullOrEmpty
        }
        It 'Should contain Tags (For the PSGallery)'{
            $ModuleInformation.Tags.count | Should not BeNullOrEmpty
        }
    }
}

InModuleScope 'PSCloudFormation' {
    Describe 'PSCloudFormation' {

        Context 'Testing New-PSCFNStack' {

            Mock -CommandName Get-CFNStack -MockWith {

                return $false
            }

            Mock -CommandName New-CFNStack -MockWith {

                return $TestStackArn
            }

            Mock -CommandName Wait-CFNStack -MockWith {

                return @{
                    StackStatus = 'CREATE_COMPLETE'
                }
            }
        }

        It 'should create stack with valid command line arguments' {

            New-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -Wait -VpcCidr 10.0.0.0/16 | Should -Be $TestStackArn
        }
    }
}