function Get-TemplateFormat
{
<#
    .SYNOPSIS
        Get template format by examining first non-whitespace char
        This is how it's done in https://github.com/aws/aws-extensions-for-dotnet-cli
#>
    param
    (
        [string]$TemplateBody
    )

    $body = $TemplateBody.Trim()

    if ($body.Length -gt 0 -and $body[0] -eq '{')
    {
        return 'JSON'
    }

    return 'YAML'
}