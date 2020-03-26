function Compress-UnixZip
{
<#
    .SYNOPSIS
        Creates a Unix-compatible zip file

    .DESCRIPTION
        The created zip is sufficiently compatible that AWS lambda will accept it.
        Unix attributes of rwxrwxrwx are set on all objects in the zip.

    .PARAMETER ZipFile
        Path to zip file to create

    .PARAMETER Path
        If this references a single file, it will be zipped.
        If this references a path, then the entire folder structure beneath the path will be zipped.

    .PARAMETER DirectoryPrefix
        If set, this becomes root directory for the entire zip content
        Used for creating lambda layers

    .PARAMETER PassThru
        If set, the path passed to -ZipFile is returned

#>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true, Position=0)]
        [string]$ZipFile,

        [Parameter(Mandatory=$true, Position=1)]
        [string]$Path,

        [string]$DirectoryPrefix,

        [switch]$PassThru
    )

    $isFolder = $false

    # Work out what to include in the zip file from the -Path argument
    $filesToZip = $(
        if (Test-Path -Path $Path -PathType Leaf)
        {
            Get-Item -Path $Path
        }
        elseif (Test-Path -Path $Path -PathType Container)
        {
            Get-ChildItem -Path $Path -Recurse
            $isFolder = $true
        }
        else
        {
            throw "Path not found: $Path"
        }
    )

    if (Test-Path -Path $ZipFile -PathType Leaf)
    {
        # Remove any pre-existing zip file
        Write-Verbose "Deleting existing package: $Zipfile"
        Remove-Item $ZipFile -Force
    }

    # Account for powershell and OS current directory not being the same
    # as .NET objects like ZipFile will use OS path
    $osZipPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ZipFile)

    if (-not $PSBoundParameters.ContainsKey('DirectoryPrefix') -or $null -eq $DirectoryPrefix)
    {
        $DirectoryPrefix = [string]::Empty
    }
    elseif ($DirectoryPrefix.Length -gt 0)
    {
        $DirectoryPrefix = $DirectoryPrefix.Trim('/', '\') + '/'
    }

    try
    {
        Write-Verbose "Creating: $ZipFile"

        # Create the zip file
        $archive = [IO.Compression.ZipFile]::Open($osZipPath, [IO.Compression.ZipArchiveMode]::Create)

        # Go to location where we are zipping for easier path resolution when creating zip directory entries
        if ($isFolder)
        {
            # Change to directory we are zipping
            Push-Location $Path
        }
        else
        {
            # Change to directory containg the file we are zipping
            Push-Location (Split-Path -Parent (Resolve-Path $Path).Path)
        }

        $totalFiles = $filesToZip.Length
        $processedFiles = 0

        Write-Progress -Activity "Creating: $ZipFile" -CurrentOperation "Zipping $Path" -PercentComplete 0
        # Add files to zip
        $filesToZip |
        Foreach-Object {
            $fullPath = Resolve-Path -Relative $_.FullName
            $isFile = Test-Path -Path $fullPath -PathType Leaf

            # Create zip directory entry name (getting rid of ./)
            $entryName = $DirectoryPrefix + $fullPath.Substring(2).Replace('\', '/')

            # Set unix attributes: rwxrwxrwx
            if ($isFile)
            {
                # Create zip file entry
                $entry = $archive.CreateEntry($entryName)
                $entry.LastWriteTime = [System.DateTimeOffset]::Now

                try
                {
                    $entry.ExternalAttributes = 0x81ff -shl 16

                    # Add file
                    $fs = [IO.File]::OpenRead($_.FullName)
                    $es = $entry.Open()
                    $fs.CopyTo($es)
                    $es.Flush()
                    Write-Verbose "Added: $($entryName)"
                }
                finally
                {
                    # Close zip entry and file read into it
                    ($es, $fs) |
                    Where-Object {
                        $null -ne $_
                    } |
                    ForEach-Object {
                        $_.Dispose()
                    }
                }
            }
            else
            {
                # Create zip directory entry
                $entry = $archive.CreateEntry($entryName + '/')
                $entry.LastWriteTime = [System.DateTimeOffset]::Now
                $entry.ExternalAttributes = (0x41ff -shl 16) -bor [int]([System.IO.FileAttributes]::Directory)
            }

            if (++$processedFiles % 10 -eq 0)
            {
                Write-Progress -Activity "Creating: $ZipFile" -CurrentOperation "Zipping $Path" -PercentComplete ($processedFiles * 100 / $totalFiles)
            }
        }

        Write-Progress -Activity "Creating: $ZipFile" -CurrentOperation "Zipping $Path" -PercentComplete 100 -Completed
    }
    finally
    {
        if ($null -ne $archive)
        {
            # Close zip file
            $archive.Dispose()
        }

        # Restore working directory
        Pop-Location
    }

    if ($PassThru)
    {
        # Return path to zip file
        $ZipFile
    }
}