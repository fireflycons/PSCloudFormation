function Resolve-NestedStack
{
<#
    .SYNOPSIS
        Resolve nested stack template for packaging

    .DESCRIPTION
        Resolve nested stack template, recursing though New-PSCFNPackage to package anything the nested stack references.
        Finally upload the modified template and return URL to it

    .PARAMETER TemplateFile
        Nested stack template to process

    .PARAMETER CallerBoundParameters
        Parameter hash passed to invocation of New-PSCFNPackage

    .PARAMETER TempFolder
        Temporary directory to use
#>
    param
    (
        [string]$TemplateFile,
        [hashtable]$CallerBoundParameters,
        [string]$TempFolder
    )

    # Create name for modified nested template
    $ext = [IO.Path]::GetExtension($TemplateFile)

    if ($script:haveCfnFlip)
    {
        # If we can flip template format
        $ext = $(
            if ($CallerBoundParameters.ContainsKey('UseJson') -and $CallerBoundParameters['UseJson'])
            {
                '.json'
            }
            else
            {
                '.yaml'
            }
        )
    }

    $templateToUpload = $TemplateFile
    $nestedOutputTemplateFile = Join-Path $TempFolder ([IO.Path]::GetFileNameWithoutExtension($TemplateFile) + $ext)

    $argumentHash = @{}

    $CallerBoundParameters.Keys |
    Where-Object {
        ('OutputTemplateFile', 'TemplateFile') -inotcontains $_
    } |
    ForEach-Object {
        $argumentHash.Add($_, $CallerBoundParameters[$_])
    }

    $argumentHash.Add('TemplateFile', $TemplateFile)
    $argumentHash.Add('OutputTemplateFile', $nestedOutputTemplateFile)

    New-PSCFNPackage @argumentHash

    if (Test-Path -Path $nestedOutputTemplateFile)
    {
        # Substitutions were made
        $templateToUpload = $nestedOutputTemplateFile
    }

    return $templateToUpload
}