if ((Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux)
{
    & pip install cfn_flip --user
}
else
{
    & pip install cfn_flip
}