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

    $defaultRegionsMap = @{
        CN  = 'cn-north-1'
        EU  = 'eu-west-1'
        GOV = 'us-gov-west-1'
        SFO = 'us-west-1'
        US  = 'us-east-1'

    }

    $bucketName = "cf-templates-pscloudformation-$(Get-CurrentRegion -CredentialArguments $CredentialArguments)-$((Get-STSCallerIdentity).Account)"

    try
    {
        $location = Get-S3BucketLocation -BucketName $bucketName @CredentialArguments | Select-Object -ExpandProperty Value

        if ($defaultRegionsMap.ContainsKey($location))
        {
            $location = $defaultRegionsMap[$location]
        }

        return New-Object psobject -Property @{
            BucketName = $bucketName
            BucketUrl  = [uri]"https://s3.$($location).amazonaws.com/$bucketName"
        }
    }
    catch
    {
        # Bucket not found
    }

    # Try to create it
    $response = New-S3Bucket -BucketName $bucketName @CredentialArguments

    if ($response)
    {
        $location = Get-S3BucketLocation -BucketName $bucketName @CredentialArguments | Select-Object -ExpandProperty Value

        if ($defaultRegionsMap.ContainsKey($location))
        {
            $location = $defaultRegionsMap[$location]
        }

        return New-Object psobject -Property @{
            BucketName = $bucketName
            BucketUrl  = [uri]"https://s3.$($location).amazonaws.com/$bucketName"
        }
    }

    throw "Unable to create S3 bucket $bucketName"
}