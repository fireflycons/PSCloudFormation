function Get-TestRegionList
{
    $isolatedRegions = @(
        'us-iso-east-1'
        'us-isob-east-1'
    )

    $regions = Get-AWSRegion | Select-Object -ExpandProperty Region

    # Remove isolated regions
    $compareResult = Compare-Object -ReferenceObject $regions -DifferenceObject $isolatedRegions -IncludeEqual

    if ($compareResult | Where-Object { $_.SideIndicator -eq '=='})
    {
        Write-Warning "Isolated regions ($($isolatedRegions -join ',')) will be ignored for purpose of these tests"
        $regions = $compareResult | Where-Object { $_.SideIndicator -eq '<=' } | Select-Object -ExpandProperty InputObject
    }

    $regions
}


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

function Compare-Templates
{
    <#
        .SYNOPSIS
            Comae generated template with expected, ignoring blank lines and line endings
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(ValueFromPipeline)]
        [string]$Template,

        [string]$ExpectedOutput
    )

    end
    {
        $templateLines = $Template -split [System.Environment]::NewLine
        $expectedLines = Get-Content $ExpectedOutput

        $result = Compare-Object -ReferenceObject $templateLines -DifferenceObject $expectedLines |
        Where-Object {
            # Ignore blank lines
            -not [string]::IsNullOrEmpty($_.InputObject)
        }

        if (($result | Measure-Object).Count -gt 0)
        {
            throw "Files are different`nExpected: $($result[0].InputObject)`nGot $($result[1].InputObject)"
        }
    }
}
