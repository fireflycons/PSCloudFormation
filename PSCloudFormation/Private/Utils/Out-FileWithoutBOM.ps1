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
    $fullPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($FilePath)

    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false

    # Write new manifest
    [IO.File]::WriteAllText($fullPath, $Content, $enc)
}