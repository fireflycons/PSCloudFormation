function New-StackOperationArguments
{
    <#
    .SYNOPSIS
        Create an argument hash of parameters needed by New-CFNStack and Update-CFNStack

    .PARAMETER StackName
        Name of stack to create/update

    .PARAMETER TemplateLocation
        May be one of
        - Local file. File is read and used to build a -TemplateBody argument
        - S3 URI (which is converted to HTTPS URI for the current region) and builds -TemplateURL argument
          Note that this only works if a default region is set in the shell and
          you don't try to point to a different region with -Region
        - https URL. URL is used as-is to build -TemplateURL argument

    .PARAMETER Capabilities
        IAM Capability for -Capability argument

    .PARAMETER StackParameters
        Array of template parameters for -Parameter argument

    .OUTPUTS
        [hashtable] Argument hash to splat New-CFNStack/Update-CFNStack
    #>
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$StackName,

        [Parameter(Mandatory = $true)]
        [string]$TemplateLocation,

        [string]$Capabilities,
        [object]$StackParameters
    )

    $stackArgs = @{

        'StackName' = $StackName
    }

    $template = New-TemplateResolver -TemplateLocation $TemplateLocation

    if ($template.IsFile)
    {
        $stackArgs.Add('TemplateBody', $template.ReadTemplate())
    }
    else
    {
        $stackArgs.Add('TemplateURL', $template.Url)
    }

    if (-not [string]::IsNullOrEmpty($Capabilities))
    {
        $stackArgs.Add('Capabilities', @($Capabilities))
    }

    if (($stackParameters | Measure-Object).Count -gt 0)
    {
        $stackArgs.Add('Parameter', $stackParameters)
    }

    $stackArgs
}
