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

        Context 'Lambda' {

            Mock -CommandName Get-S3Object -MockWith {
                throw "The specified bucket does not exist"
            }

            Mock -CommandName Get-S3Bucket -MockWith { }

            Mock -CommandName New-S3Bucket -MockWith { }

            Mock -CommandName Write-S3Object -MockWith { }

            ('json', 'yaml') |
            Foreach-Object {

                $ext = $_
                $assetsDir = [IO.Path]::Combine($TestRoot, 'packager', 'lambdafunction')

                It "Should process simple lambda: $_" {

                    $inputFile = Join-Path $assetsDir ('lambdasimple.' + $ext)
                    $expectedOutput = Join-Path $assetsDir lambdasimple-expected.yaml

                    $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket)
                    $expectedOutput | Should -FileContentMatchMultiline $template
                }

                It "Should process complex (in a directory) lambda: $_" {

                    $inputFile = Join-Path $assetsDir ('lambdacomplex.' + $ext)
                    $expectedOutput = Join-Path $assetsDir lambdacomplex-expected.yaml

                    $template = Format-Yaml -Template (New-PSCFNPackage -TemplateFile $inputFile -S3Bucket my-bucket)
                    $expectedOutput | Should -FileContentMatchMultiline $template
                }
            }
        }
    }
}