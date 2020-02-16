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

$global:TestRoot = $PSScriptRoot

# Import the module
Import-Module -Name $ManifestFile

InModuleScope $ModuleName {

    . (Join-Path $PSScriptRoot TestHelpers.ps1)

    Describe 'PSCloudFormation - Packaging' {

        . (Join-Path $global:TestRoot MockS3.class.ps1)

        $mockS3 = [MockS3]::UseS3Mocks()

        ('json', 'yaml') |
        Foreach-Object {

            $ext = $_

            Context "Simple Lambda ($ext)" {

                $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'lambdafunction')

                $inputFile = Join-Path $assetsDir ('lambdasimple.' + $ext)
                $expectedOutput = Join-Path $assetsDir "lambdasimple-expected.yaml"

                $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket -Console)

                It "Zipped package should exist" {

                    "TestDrive:/my-bucket/lambdasimple.zip" | Should -Exist
                }

                It "Processed template should have expected properties" {

                    $template | Compare-Templates -ExpectedOutput $expectedOutput
                }
            }

            Context "Complex Lambda - in a directory ($ext)" {

                $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'lambdafunction')

                $inputFile = Join-Path $assetsDir ('lambdacomplex.' + $ext)
                $expectedOutput = Join-Path $assetsDir "lambdacomplex-expected.yaml"

                $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket -Console)

                It "Zipped package should exist" {

                    "TestDrive:/my-bucket/lambdacomplex.zip" | Should -Exist
                }

                It "Processed template should have expected properties" {

                    $template | Compare-Templates -ExpectedOutput $expectedOutput
                }
            }

            Context "Glue ($ext)"  {

                $ext = $_
                $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'glue')

                $inputFile = Join-Path $assetsDir ('glue.' + $ext)
                $expectedOutput = Join-Path $assetsDir "glue-expected.yaml"

                $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket -Console)


                It "Zipped package should exist" {

                    "TestDrive:/my-bucket/glue.py" | Should -Exist
                }

                It "Processed template should have expected properties" {

                    $template | Compare-Templates -ExpectedOutput $expectedOutput
                }
            }
        }

        Context 'With Metadata' {

            $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'glue')

            It 'Should upload artifacts with metadata' {

                $inputFile = Join-Path $assetsDir 'glue.yaml'
                New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket -Metadata @{ 'data1' = 'value1' } -Console | Out-Null

                $response = Get-S3ObjectMetadata -BucketName my-bucket -key glue.py
                $response.Metadata['x-amz-meta-data1'] | Should -Be 'value1'
            }
        }

        Context 'Nested Stacks' {

            $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'complex-nested-stacks')

            It 'Should process multi-level neested stack' {

                $inputFile = Join-Path $assetsDir 'base-stack.json'
                $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket -Console)
                "TestDrive:/my-bucket/nested-1.yaml" | Should -Exist
                "TestDrive:/my-bucket/sub-nested-2.yaml" | Should -Exist
                "TestDrive:/my-bucket/lambdasimple.zip" | Should -Exist

            }
        }

        Context 'Nested Stacks With -UseJson' {

            $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'complex-nested-stacks')

            It 'Should process multi-level neested stack' {

                $inputFile = Join-Path $assetsDir 'base-stack.json'
                $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket -UseJson -Console)
                Get-FileFormat -TemplateBody $template | Should -Be 'JSON'
                "TestDrive:/my-bucket/nested-1.json" | Should -Exist
                "TestDrive:/my-bucket/sub-nested-2.json" | Should -Exist
                "TestDrive:/my-bucket/lambdasimple.zip" | Should -Exist
            }
        }
    }
}