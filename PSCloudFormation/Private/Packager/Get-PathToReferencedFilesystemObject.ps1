function Get-PathToReferencedFilesystemObject
{
    param
    (
        [string]$ParentTemplate,
        [string]$ReferencedFileSystemObject
    )

    if ([IO.Path]::IsPathRooted($ReferencedFileSystemObject))
    {
        # Will do an existence check
        (Resolve-Path $ReferencedFileSystemObject).Path
    }
    else
    {
        # Work out path of object relative to current template
        (Resolve-Path -Path (Join-Path ([IO.Path]::GetDirectoryName($ParentTemplate)) $ReferencedFileSystemObject)).Path
    }
}

