function Get-CloudFormationBucket
{
    <#
    .SYNOPSIS
        Returns, creating if necessary bucket to use for uploading oversize templates.

    .PARAMETER CredentialArguments
        Credential arguments passed to public function.

    .OUTPUTS
        [PSObject] with the following fields
        - BucketName
        - BucketUrl
#>
    param
    (
        [hashtable]$CredentialArguments
    )

    # To support localstack testing, we have to fudge EndpointURL if present
    $s3Arguments = Update-EndpointValue -CredentialArguments $CredentialArguments -Service S3
    $bucketName = "cf-templates-pscloudformation-$(Get-CurrentRegion -CredentialArguments $s3Arguments)-$((Get-STSCallerIdentity @CredentialArguments).Account)"

    if (Get-S3Bucket -BucketName $bucketName @s3Arguments)
    {
        Write-CloudFormationBucketTagging -BucketName $bucketName -CredentialArguments $s3Arguments

        return New-Object psobject -Property @{
            BucketName = $bucketName
            BucketUrl  = [uri]"https://$($bucketName).s3.amazonaws.com"
        }
    }

    # Try to create it
    $response = New-S3Bucket -BucketName $bucketName @s3Arguments

    if ($response)
    {
        Write-Host "Created S3 bucket $bucketName to store oversize templates."
        Write-CloudFormationBucketTagging -BucketName $bucketName -CredentialArguments $s3Arguments

        return New-Object psobject -Property @{
            BucketName = $bucketName
            BucketUrl  = [uri]"https://$($bucketName).s3.amazonaws.com"
        }
    }

    throw "Unable to create S3 bucket $bucketName"
}