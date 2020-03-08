#$ModuleName = $(
#    if ($PSVersionTable.PSEdition -ieq 'Core')
#    {
#        'PSCloudFormation.netcore'
#    }
#    else
#    {
#        'PSCloudFormation'
#    }
#)

$ModuleName = 'PSCloudFormation'

# http://www.lazywinadmin.com/2016/05/using-pester-to-test-your-manifest-file.html
# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = Get-ChildItem -Path (Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition)) -Recurse -Filter "$ModuleName.psd1" | Select-Object -ExpandProperty FullName

if (($ManifestFile | Measure-Object).Count -ne 1)
{
    throw "Cannot locate $ModuleName.psd1"
}

$global:TestRoot = $PSScriptRoot

Import-Module -Name $ManifestFile

InModuleScope $ModuleName {

    . (Join-Path $PSScriptRoot TestHelpers.ps1)

    Describe 'Parse Resource Imports file' {

        $cf = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object Location -Match "AWSSDK.Core.dll"

        if (-not ($cf.GetTypes() | Where-Object { $_.Name -ieq 'AWSPropertyAttribute' }))
        {
            It 'Is Inconclusive' {

                Set-ItResult -Inconclusive -Because "an incompatible version of AWSSDK.Core.dll was loaded from $($cf.Location) before we could import the correct one."
            }
        }
        else
        {
            ('json', 'yaml') |
            Foreach-Object {

                $ext = $_

                Context "Load resource imports ($ext)" {

                    $resouceFile = [IO.Path]::Combine($TestRoot, 'resource-import', "resources.$ext")
                    $resourceImports = New-ResourceImports($resouceFile)

                    It 'Has loaded two resource import decriptions' {

                        $resourceImports | Should -HaveCount 2
                    }

                    $bucketResource = $resourceImports | Where-Object { $_.ResourceType -eq 'AWS::S3::Bucket'}

                    It 'Should have imported a bucket resource' {

                        $bucketResource | Should -HaveCount 1
                    }

                    It 'Bucket resource should have expected logical ID' {

                        $bucketResource.LogicalResourceId | Should -Be 'ApplicationBucket'
                    }

                    It 'Bucket resource should have expected BucketName identifier' {

                        $bucketResource.ResourceIdentifier['BucketName'] | Should -Be 'MyBucket'
                    }

                    $securityGroupResource = $resourceImports | Where-Object { $_.ResourceType -eq 'AWS::EC2::SecurityGroup'}

                    It 'Should have imported a security group resource' {

                        $securityGroupResource | Should -HaveCount 1
                    }

                    It 'Security group resource should have expected logical ID' {

                        $securityGroupResource.LogicalResourceId | Should -Be 'InstanceSecurityGroup'
                    }

                    It 'Security group resource should have expected BucketName identifier' {

                        $securityGroupResource.ResourceIdentifier['SecurityGroupName'] | Should -Be 'MySg'
                    }
                }
            }
        }
    }
}