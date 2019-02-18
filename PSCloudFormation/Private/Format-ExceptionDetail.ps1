function Format-ExceptionDetail
{
    param
    (
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    $ex = $_.Exception
    $indent = 0

    while ($ex)
    {
        Write-Host -ForegroundColor Red -BackgroundColor Black "$(" " * $indent)$($ex.GetType().Name): $($ex.Message)"
        $ex = $ex.InnerException
        $indent += 2
    }
}