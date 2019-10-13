if ((Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux)
{
    & pip install cfn_flip --user 2>&1
}
else
{
    & pip install cfn_flip 2>&1
}

# Check installation
if (-not ((Get-Command -Name cfn-flip.exe -ErrorAction SilentlyContinue) -or (Get-Command -Name cfn-flip -ErrorAction SilentlyContinue)))
{
    throw "Cannot find cfn-flip"
}


