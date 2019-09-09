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

    .OUTPUTS
        S3 URL of uploaded nested stack template.
#>
    param
    (
        [string]$TemplateFile,
        [hashtable]$CallerBoundParameters
    )

    # Create name for modified nested template
    $ext = $(
        if ($UseJson)
        {
            '.json'
        }
        else
        {
            '.yaml'
        }
    )

    $credentialParameters = Get-CommonCredentialParameters -CallerBoundParameters $CallerBoundParameters

    $templateToUpload = $TemplateFile
    $nestedOutputTemplateFile = Join-Path ([IO.Path]::GetDirectoryName($TemplateFile)) ([IO.Path]::GetFileNameWithoutExtension($TemplateFile) + "-packaged" + $ext)

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

    # Now upload template to S3
    $bucket = $CallerBoundParameters['S3Bucket']
    $key = $(
        if ($CallerBoundParameters.ContainsKey('S3Prefix'))
        {
            $CallerBoundParameters['S3Prefix'] + '/' + [IO.Path]::GetFileName($templateToUpload)
        }
        else
        {
            [IO.Path]::GetFileName($templateToUpload)
        }
    )

    $region = Get-CurrentRegion -CredentialArguments $credentialParameters

    return "https://s3.$($region).amazonaws.com/$bucket/$key"
}