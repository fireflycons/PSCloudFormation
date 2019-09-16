function Out-FileWithoutBOM
{
<#
    .SYNOPSIS
        Save text file without byte ordering mark
#>
    [CmdletBinding()]
    param
    (
        [Parameter(ValueFromPipeline)]
        [string]$Content,

        [string]$FilePath
    )

    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false

    # Write new manifest
    [IO.File]::WriteAllText($FilePath, $Content, $enc)
}