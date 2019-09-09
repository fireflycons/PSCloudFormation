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

        [switch]$Force,

        [hashtable]$CredentialArguments,

        [Parameter(ParameterSetName = 'json')]
        [switch]$Json,

        [Parameter(ParameterSetName = 'yaml')]
        [switch]$Yaml
    )

    $typeUsesBundle = ($ResourceType -eq 'AWS::Lambda::Function' -or $ResourceType -eq 'AWS::ElasticBeanstalk::ApplicationVersion')

    if (Test-Path -Path $Payload -PathType Leaf)
    {
        if ($typeUsesBundle)
        {
            # All lambda deployments must be zipped
            $zipFile = [IO.Path]::GetFileNameWithoutExtension($Payload) + ".zip"

            $n = $null
            if ($Json)
            {
                $n = New-S3BundleNode -Json -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile
            }
            else
            {
                $n = (New-S3BundleNode -Yaml -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile).MappingNode
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

        $artifactDetail =  New-Object PSObject -Property @{
            Artifact = $zipFile
            Zip = $true
            Value = $(
                if ($typeUsesBundle)
                {
                    if ($Json)
                    {
                        New-S3BundleNode -Json -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile
                    }
                    else
                    {
                        (New-S3BundleNode -Yaml -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile).MappingNode
                    }
                }
                else
                {
                    New-S3ObjectUrl -Bucket $Bucket -Prefix $Prefix -Artifact $zipFile
                }
            )
            Uploaded = $false
        }
    }

    $tmpPath = $null
    $fileToUpload = $referencedFileSystemObject

    try
    {
        # Create zip if needed in a unique temp dir to prevent overwriting anything existing
        if ($artifactDetail.Zip)
        {
            $tmpPath = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().Guid)

            New-Item -Path $tmpPath -ItemType Directory | Out-Null
            $fileToUpload = Join-Path $tmpPath $artifactDetail.Artifact
            Compress-UnixZip -ZipFile $fileToUpload -Path $referencedFileSystemObject
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

        $artifactDetail.Uploaded = Write-S3PackageArtifact -Bucket $Bucket -Key $s3Key -Path $fileToUpload -Force:$Force -CredentialArguments $CredentialArguments
    }
    finally
    {
        if ($null -ne $tmpPath)
        {
            Remove-Item $tmpPath -Recurse -Force
        }
    }

    return $artifactDetail
}