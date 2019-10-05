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

$global:TestRoot = $PSScriptRoot

# Import the module
Import-Module -Name $ManifestFile

InModuleScope $ModuleName {

    function Format-Yaml
    {
    <#
        .SYNOPSIS
            Pass a YAML template (e.g. from cfn-flip) through YamlDotNet to reformat it

    #>
        param
        (
            [string]$Template
        )

        $yaml = New-Object YamlDotNet.RepresentationModel.YamlStream

        try
        {
            $input = New-Object System.IO.StringReader($Template)
            $yaml.Load($input)
        }
        finally
        {
            if ($null -ne $input)
            {
                $input.Dispose()
            }
        }

        try
        {
            $output = New-Object System.IO.StringWriter
            $yaml.Save($output, $false)
            $output.ToString()
        }
        finally
        {
            if ($null -ne $output)
            {
                $output.Dispose()
            }
        }
    }

    Describe 'PSCloudFormation - Packaging' {

        . (Join-Path $global:TestRoot MockS3.class.ps1)

        $mockS3 = [MockS3]::UseS3Mocks()

        Context 'Lambda' {

            ('json', 'yaml') |
            Foreach-Object {

                $ext = $_
                $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'lambdafunction')

                It "Should process simple lambda: $_" {

                    $inputFile = Join-Path $assetsDir ('lambdasimple.' + $ext)
                    $expectedOutput = Join-Path $assetsDir lambdasimple-expected.yaml

                    $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket)
                    "TestDrive:/my-bucket/lambdasimple.zip" | Should -Exist
                    $template | Should -Be (Get-Content -Raw $expectedOutput)
                }

                It "Should process complex (in a directory) lambda: $_" {

                    $inputFile = Join-Path $assetsDir ('lambdacomplex.' + $ext)
                    $expectedOutput = Join-Path $assetsDir lambdacomplex-expected.yaml

                    $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket)
                    "TestDrive:/my-bucket/lambdacomplex.zip" | Should -Exist
                    $template | Should -Be (Get-Content -Raw $expectedOutput)
                }
            }
        }

        Context 'Glue' {

            ('json', 'yaml') |
            Foreach-Object {

                $ext = $_
                $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'glue')

                It "Should process glue job: $_" {

                    $inputFile = Join-Path $assetsDir ('glue.' + $ext)
                    $expectedOutput = Join-Path $assetsDir glue-expected.yaml

                    $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket)
                    "TestDrive:/my-bucket/glue.py" | Should -Exist
                    $template | Should -Be (Get-Content -Raw $expectedOutput)
                }
            }
        }

        Context 'With Metadata' {

            $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'glue')

            It 'Should upload artifacts with metadata' {

                $inputFile = Join-Path $assetsDir 'glue.yaml'
                New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket -Metadata @{ 'data1' = 'value1' } | Out-Null

                $response = Get-S3ObjectMetadata -BucketName my-bucket -key glue.py
                $response.Metadata['x-amz-meta-data1'] | Should -Be 'value1'
            }
        }

        Context 'Nested Stacks' {

            $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'complex-nested-stacks')

            It 'Should process multi-level neested stack' {

                $inputFile = Join-Path $assetsDir 'base-stack.json'
                $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket)
                "TestDrive:/my-bucket/nested-1.yaml" | Should -Exist
                "TestDrive:/my-bucket/sub-nested-2.yaml" | Should -Exist
                "TestDrive:/my-bucket/lambdasimple.zip" | Should -Exist

            }
        }

        Context 'Nested Stacks With -UseJson' {

            $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'complex-nested-stacks')

            It 'Should process multi-level neested stack' {

                $inputFile = Join-Path $assetsDir 'base-stack.json'
                $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket -UseJson)
                Get-TemplateFormat -TemplateBody $template | Should -Be 'JSON'
                "TestDrive:/my-bucket/nested-1.json" | Should -Exist
                "TestDrive:/my-bucket/sub-nested-2.json" | Should -Exist
                "TestDrive:/my-bucket/lambdasimple.zip" | Should -Exist
            }
        }
    }
}