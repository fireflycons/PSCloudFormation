if ((Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux)
{
    & pip install cfn_flip --user 2>&1
}
else
{
    & pip install cfn_flip 2>&1
}