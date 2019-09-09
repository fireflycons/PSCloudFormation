function New-S3ObjectUrl
{
    param
    (
        [string]$Bucket,
        [string]$Prefix,
        [string]$Artifact
    )

    $filename = [IO.Path]::GetFileName($Artifact)

    if ([string]::IsNullOrEmpty($prefix))
    {
        "https://$($Bucket).s3.amazonaws.com/$($filename)"
    }
    else
    {
        "https://$($Bucket).s3.amazonaws.com/$($Prefix.TrimEnd('/', '\'))/$($filename)"
    }
}

