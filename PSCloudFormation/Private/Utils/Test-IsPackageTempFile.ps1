function Test-IsPackageTempFile
{
<#
    .SYNOPSIS
        Test if a template file was created by New-PSCFNPackage -PassThru
#>
    param
    (
        [string]$TemplateFile
    )

    try
    {
        $fullPath = (Resolve-Path $TemplateFile).Path
        $rx = Join-Path ([IO.Path]::GetTempPath().Replace('\','\\')) 'pscloudformation-(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}.tmp'

        $fullPath -match $rx
    }
    catch
    {
        $false
    }
}