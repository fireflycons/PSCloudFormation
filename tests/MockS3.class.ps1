
class MockS3
{
    [string]$rootDir

    MockS3([string]$RootDir)
    {
        $this.rootDir = $RootDir
    }

    [Amazon.S3.Model.S3Bucket]NewBucket([string]$bucketName)
    {
        if ($this.BucketExists($bucketName))
        {
            throw "Your previous request to create the named bucket succeeded and you already own it."
        }

        $path = $this.BucketPath($bucketName)

        New-Item -Path $path -ItemType Directory | Out-Null

        New-Object PSObject -Property @{
            Tags = @{}
        } |
        ConvertTo-Json |
        Out-File -FilePath (Join-Path $path '.bucket-metadata')

        return New-Object Amazon.S3.Model.S3Bucket -Property @{
            BucketName = $bucketName
            CreationDate = [DateTime]::Now
        }
    }

    [System.Collections.Generic.List[Amazon.S3.Model.S3Bucket]]GetBucket([string]$bucketName)
    {
        [scriptblock]$filter = $(

            if ([string]::IsNullOrEmpty($bucketName))
            {
                { $true }
            }
            else
            {
                { $_.Name -eq $bucketName }
            }
        )

        $retval = [System.Collections.Generic.List[Amazon.S3.Model.S3Bucket]]::new()

        Get-ChildItem -Path $this.rootDir -Directory |
        Where-Object $filter |
        ForEach-Object {

            $retval.Add((
                New-Object Amazon.S3.Model.S3Bucket -Property @{
                    BucketName = $bucketName
                    CreationDate = $_.CreationTime
                }
            ))
        }

        return $retval
    }

    [System.Collections.Generic.List[Amazon.S3.Model.S3Object]]GetObject([string]$bucketName, [string]$keyPrefix, [string]$key)
    {
        if (-not $this.BucketExists($bucketName))
        {
            throw 'The specified bucket does not exist'
        }

        [System.Collections.Generic.List[Amazon.S3.Model.S3Object]]$objects = [System.Collections.Generic.List[Amazon.S3.Model.S3Object]]::new()

        if (-not [string]::IsNullOrEmpty($key))
        {
            # -KeyPrefix and -Key have the same effect, but are different parameter sets
            $keyPrefix = $key
        }

        if ([string]::IsNullOrEmpty($keyPrefix))
        {
            $keyPrefix = [string]::Empty
        }

        $keyPrefix = $keyPrefix.Replace('/', [IO.Path]::DirectorySeparatorChar).TrimStart([IO.Path]::DirectorySeparatorChar)

        Push-Location $this.BucketPath($bucketName)

        try
        {
            Get-ChildItem -Recurse |
            Where-Object {
                if ($_.Extension -ieq '.metadata' -or $_.name -ieq '.bucket-metadata')
                {
                    $false
                }
                elseif ([string]::IsNullOrEmpty($keyPrefix))
                {
                    # All items
                    $true
                }
                else
                {
                    (Resolve-Path -Relative $_.FullName).TrimStart('.', '/', '\').TrimEnd('/', '\').StartsWith($keyPrefix)
                }
            } |
            ForEach-Object {
                $k = (Resolve-Path -Relative $_.FullName).TrimStart('.', '/', '\').TrimEnd('/', '\')
                $etag = ''
                $size = 0

                if ($_ -is [IO.DirectoryInfo])
                {
                    $k = $k + '/'
                    $etag = '"d41d8cd98f00b204e9800998ecf8427e"'
                }
                else
                {
                    $etag = (Get-FileHash -Algorithm MD5 -Path $_.FullName).Hash.ToLower()
                    $size = $_.Length
                }

                $k = $k.Replace('\', '/')

                $objects.Add((
                    New-Object Amazon.S3.Model.S3Object -Property @{
                        BucketName = $bucketName
                        Key = $k
                        LastModified = $_.LastWriteTime
                        Size = $size
                        ETag = '"' + $etag + '"'
                    })
                )
            }

            return $objects
        }
        finally
        {
            Pop-Location
        }
    }

    [object]GetObjectMetadata([string]$bucketName, [string]$key)
    {
        if (-not $this.BucketExists($bucketName))
        {
            throw 'The specified bucket does not exist'
        }

        if ([string]::IsNullOrEmpty($key))
        {
            $key = [string]::Empty
        }

        $key = $key.Replace('/', [IO.Path]::DirectorySeparatorChar).TrimStart([IO.Path]::DirectorySeparatorChar)

        $metadataFile = Join-Path $this.BucketPath($bucketName) "$($key).metadata"

        if (-Not (Test-Path -Path $metadataFile -PathType Leaf))
        {
            throw "Get-S3ObjectMetadata : One or more errors occurred. (Error making request with Error Code NotFound and Http Status Code NotFound. No further error information was returned by the service.)"
        }

        $metadata = Get-Content -Raw -Path $metadataFile | ConvertFrom-Json

        $retval = New-Object Amazon.S3.Model.MetadataCollection
        $metadata.MetaData.PSObject.Properties |
        Foreach-Object {
            $retval.Add("x-amz-meta-$($_.Name.ToLower())", $_.Value)
        }

        return New-Object PSobject -Property @{
            Metadata = $retval
        }
    }

    [void]WriteObject([string]$bucketName, [string]$key, [string]$file, [hashtable]$metadata = @{})
    {
        if (-not $this.BucketExists($bucketName))
        {
            throw 'The specified bucket does not exist'
        }

        $key = $key.TrimStart('\', '/')
        $target = Join-Path $this.BucketPath($bucketName) $key

        if ($key.EndsWith('/'))
        {
            # Write 'folder'
            throw New-Object NotSupportedException -ArgumentList "Folder-like keys containing content not supported"
        }
        else
        {
            $data = $target + '.metadata'

            $dir = [IO.Path]::GetDirectoryName($target)

            if (-not (Test-Path -Path $dir -PathType Container))
            {
                New-Item -Path $dir -ItemType Directory | Out-Null
            }

            Copy-Item -Path $file -Destination $target -Force

            # 'Touch' the file
            $dt = Get-Date
            $f = Get-ChildItem $target
            $f.LastWriteTime = $dt
            $f.CreationTime = $dt
            $f.LastAccessTime = $dt

            # Write metadata
            New-Object PSObject -Property @{
                MetaData = $metadata
            } |
            ConvertTo-Json |
            Out-File -FilePath $data
        }
    }

    [System.Collections.Generic.List[Amazon.S3.Model.Tag]]GetBucketTagging([string]$bucketName)
    {
        if (-not $this.BucketExists($bucketName))
        {
            throw 'The specified bucket does not exist'
        }

        $metadataFile = Join-Path $this.BucketPath($bucketName) '.bucket-metadata'
        $metadata = Get-Content -Raw $metadataFile | ConvertFrom-Json

        $retval = [System.Collections.Generic.List[Amazon.S3.Model.Tag]]::new()

        $metadata.Tags.PSObject.Properties |
        Foreach-Object {
            $retval.Add((
                New-Object Amazon.S3.Model.Tag -Property @{
                    Key = $_.Name
                    Value = $_.Value
                }
            ))
        }

        return $retval
    }

    [void]WriteBucketTagging([string]$bucketName, [Amazon.S3.Model.Tag[]]$tagSet)
    {
        if (-not $this.BucketExists($bucketName))
        {
            throw 'The specified bucket does not exist'
        }

        $metadataFile = Join-Path $this.BucketPath($bucketName) '.bucket-metadata'
        $metadata = Get-Content -Raw $metadataFile | ConvertFrom-Json

        # If you use the PUT Bucket tagging to add a set of tags to an existing bucket, any existing tag set will be overwritten.
        $metadata.Tags = @{}

        $tagSet |
        Foreach-Object {
            $metadata.Tags.Add($_.Key, $_.Value)
        }

        $metadata |
        ConvertTo-Json |
        Out-File -FilePath $metadataFile
    }

    static [MockS3]UseS3Mocks()
    {
        return Invoke-Command -NoNewScope {

            $mockS3 = $mockS3 = [MockS3]::new($testdrive)

            Mock -CommandName New-S3Bucket -MockWith {
                $mockS3.NewBucket($BucketName)
            }

            Mock -CommandName Get-S3Bucket -MockWith {
                $mockS3.GetBucket($BucketName)
            }

            Mock -CommandName Write-S3Object {

                $mockS3.WriteObject($BucketName, $Key, $File, $Metadata)
            }

            Mock -CommandName Get-S3Object -MockWith {

                $mockS3.GetObject($BucketName, $KeyPrefix, $Key)
            }

            Mock -CommandName Get-S3ObjectMetadata -MockWith {

                $mockS3.GetObjectMetadata($BucketName, $Key)
            }

            Mock -CommandName Write-S3BucketTagging -MockWith {

                $mockS3.WriteBucketTagging($BucketName, $TagSet)
            }

            Mock -CommandName Get-S3BucketTagging -MockWith {

                $mockS3.GetBucketTagging($BucketName)
            }

            # Throw undefined for all remaining S3 operations
            $implemented = ('New-S3Bucket', 'Get-S3Bucket', 'Write-S3Object', 'Get-S3Object', 'Get-S3ObjectMetadata', 'Write-S3BucketTagging', 'Get-S3BucketTagging')

            Get-AWSCmdletName -Service S3 |
            Select-Object -ExpandProperty CmdletName |
            Where-Object {
                -not [string]::IsNullOrEmpty($_) -and $implemented -inotcontains $_
            } |
            ForEach-Object {

                $cmdlet = $_
                Write-Host "Generating NotImplemented mock for $cmdlet"

                try
                {
                    Mock -CommandName $cmdlet -MockWith {
                        throw (New-Object NotImplementedException -ArgumentList "$cmdlet not implemented")
                    }
                }
                catch
                {
                    Write-Warning "Unable to generate NotImplemented mock for $cmdlet"
                }
            }

            $mockS3
        }

    }

    hidden [bool]BucketExists([string]$bucketName)
    {
        return (Test-Path -Path $this.BucketPath($bucketName) -PathType Container)
    }

    hidden [string]BucketPath([string]$bucketName)
    {
        return Join-Path $this.rootDir $bucketName
    }
}
