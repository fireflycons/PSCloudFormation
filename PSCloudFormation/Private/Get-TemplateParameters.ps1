function Get-TemplateParameters
{
    <#
    .SYNOPSIS
        Extract template parameter block as a PowerShell object graph.

    .PARAMETER TemplateResolver
        A resolver object returned by New-TemplateResolver.

    .OUTPUTS
        [object] Parameter block deserialised from JSON or YAML,
                    or nothing if template has no parameters.
    #>
    param
    (
        [object]$TemplateResolver
    )

    $template = $TemplateResolver.ReadTemplate()

    # Check YAML/JSON
    try
    {
        $templateObject = $template | ConvertFrom-Json

        if ($templateObject.PSObject.Properties.Name -contains 'Parameters')
        {
            return $templateObject.Parameters
        }
        else
        {
            # No parameters
            return
        }
    }
    catch
    {
        if (-not $Script:yamlSupport)
        {
            throw "Template cannot be parsed as JSON and YAML support unavailable"
        }
    }

    # Try YAML - convert it through JSON and back to ensure the final object looks the same as one converted directly from JSON
    $templateObject = $template | ConvertFrom-Yaml | ConvertTo-Json | ConvertFrom-Json

    if ($templateObject.PSObject.Properties.Name -contains 'Parameters')
    {
        return $templateObject.Parameters
    }
}
