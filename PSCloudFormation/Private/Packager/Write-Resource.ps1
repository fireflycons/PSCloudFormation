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

        if ($typeUsesBundle)
        {
            if ($Json)
            {
                $v = New-S3BundleNode -Json -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile
            }
            else
            {
                $v = (New-S3BundleNode -Yaml -Bucket $Bucket -Prefix $Prefix -ArtifactZip $zipFile).MappingNode
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
        $fileToUpload = Join-Path $TempFolder $artifactDetail.Artifact
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

    $artifactDetail.Uploaded = Write-S3PackageArtifact -Bucket $Bucket -Key $s3Key -Path $fileToUpload -Force:$Force -CredentialArguments $CredentialArguments -Metadata $Metadata -KmsKeyId $KmsKeyId

    return $artifactDetail
}