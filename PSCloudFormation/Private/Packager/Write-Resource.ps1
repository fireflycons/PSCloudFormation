function Write-Resource
{
<#
    .SYNOPSIS
        Resolve packaged resource and upload returning name of artifact to upload and new item in template object graph

#>
    param
    (
        [string]$Payload,

        [string]$ResourceType,

        [string]$Bucket,

        [string]$Prefix,

        [string]$TempFolder,

        [hashtable]$Metadata,

        [string]$KmsKeyId,

        [switch]$Force,

        [hashtable]$CredentialArguments,

        [Parameter(ParameterSetName = 'json')]
        [switch]$Json,

        [Parameter(ParameterSetName = 'yaml')]
        [switch]$Yaml
    )

    $bundledTypes = @(
        'AWS::Lambda::Function'
        'AWS::ElasticBeanstalk::ApplicationVersion'
        'AWS::Serverless::Function'
        'AWS::Lambda::LayerVersion'
    )

    $typeUsesBundle = $bundledTypes -contains $ResourceType
    $bundleType = $(

        if ($ResourceType -eq 'AWS::Serverless::Function')
        {
            'ServerlessFunction'
        }
        else
        {
            'Standard'
        }
    )

    if (Test-Path -Path $Payload -PathType Leaf)
    {
        if ($typeUsesBundle)
        {
            # All lambda deployments must be zipped
            $zipFile = [IO.Path]::GetFileNameWithoutExtension($Payload) + ".zip"

            $n = $null
            if ($Json)
            {
                $n = New-S3BundleNode -Json -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile -BundleType $bundleType
            }
            else
            {
                $n = (New-S3BundleNode -Yaml -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile -BundleType $bundleType).MappingNode
            }

            $artifactDetail = New-Object PSObject -Property @{
                Artifact = $zipFile
                Zip = $true
                Value = $n
                Uploaded = $false
            }
        }
        else
        {
            # Artifact is a single file - this will be uploaded directly.
            $artifactDetail =  New-Object PSObject -Property @{
                Artifact = $Payload
                Zip = $false
                Value = New-S3ObjectUrl -Bucket $Bucket -Prefix $Prefix -Artifact $Payload
                Uploaded = $false
            }
        }
    }
    else
    {
        # Artifact is a directory. Must be zipped.
        $zipFile = [IO.Path]::GetFileName($Payload) + ".zip"

        if ($typeUsesBundle)
        {
            if ($Json)
            {
                $v = New-S3BundleNode -Json -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile -BundleType $bundleType
            }
            else
            {
                $v = (New-S3BundleNode -Yaml -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile -BundleType $bundleType).MappingNode
            }
        }
        else
        {
            $v = New-S3ObjectUrl -Bucket $Bucket -Prefix $Prefix -Artifact $zipFile
        }

        $artifactDetail =  New-Object PSObject -Property @{
            Artifact = $zipFile
            Zip = $true
            Value = $v
            Uploaded = $false
        }
    }

    $fileToUpload = $referencedFileSystemObject

    # Create zip if needed in a unique temp dir to prevent overwriting anything existing
    if ($artifactDetail.Zip)
    {
        $directoryPrefix = $null

        if ($ResourceType -eq 'AWS::Lambda::LayerVersion')
        {
            # Guess layer language
            # EXPERIMENTAL - We don't expect mixed languages within a dependency directory structure

            $prevalentFiletype = Get-ChildItem -Path $referencedFileSystemObject -File -Recurse |
            Group-Object -Property Extension |
            Select-Object Count, Name |
            Sort-Object -Descending Count |
            Select-Object -First 1 |
            Select-Object -ExpandProperty Name

            $directoryPrefix = Invoke-Command -NoNewScope {
                if ($prevalentFiletype -imatch '^\.py[cdi]?$')
                {
                    'python'
                }
                elseif ($prevalentFiletype -ieq '.js')
                {
                    'nodejs'
                }
                elseif ($prevalentFiletype -ieq '.jar')
                {
                    'java'
                }
                elseif ($prevalentFileType -ieq '.lib')
                {
                    'lib'
                }
                else
                {
                    'bin'
                }
            }

            Write-Host "$($ResourceType): Assuming '$directoryPrefix' from content analysis of '$referencedFileSystemObject'"
        }

        $fileToUpload = Join-Path $TempFolder $artifactDetail.Artifact
        Compress-UnixZip -ZipFile $fileToUpload -Path $referencedFileSystemObject -DirectoryPrefix $directoryPrefix
    }

    # Now write
    $s3Key = $(

        $f = [IO.Path]::GetFileName($fileToUpload)
        if ([string]::IsNullOrEmpty($Prefix))
        {
            $f
        }
        else
        {
            $Prefix.Trim('/') + '/' + $f
        }
    )

    $artifactDetail.Uploaded = Write-S3PackageArtifact -Bucket $Bucket -Key $s3Key -Path $fileToUpload -Force:$Force -CredentialArguments $CredentialArguments -Metadata $Metadata -KmsKeyId $KmsKeyId

    return $artifactDetail
}