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

    # Handle difference between provider path and .NET path
    $fullPath = $(
        try
        {
            (Resolve-Path -Path $FilePath).Path
        }
        catch
        {
            New-Item -Path $FilePath -ItemType File | Out-Null
            (Resolve-Path -Path $FilePath).Path
        }
    )

    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false

    # Write new manifest
    [IO.File]::WriteAllText($fullPath, $Content, $enc)
}