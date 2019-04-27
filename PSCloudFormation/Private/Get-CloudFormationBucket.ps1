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

    $bucketName = "cf-templates-pscloudformation-$(Get-CurrentRegion -CredentialArguments $CredentialArguments)-$((Get-STSCallerIdentity @CredentialArguments).Account)"

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
        Write-Host "Created S3 bucket $bucketName to store oversize templates."
        try
        {
            $module = (Get-Command (Get-PSCallStack | Select-Object -First 1).Command).Module
            Write-S3BucketTagging -BucketName $bucketName @CredentialArguments -TagSet @(
                @{
                    Key   = 'CreatedBy'
                    Value = $module.Name
                }
                @{
                    Key   = 'ProjectURL'
                    Value = $module.ProjectUri.ToString()
                }
                @{
                    Key   = 'Purpose'
                    Value = 'CloudFormation templates larger than 51200 bytes'
                }
                @{
                    Key   = 'DeletionPolicy'
                    Value = 'Safe to delete objects more than 1 hour old'
                }
            )
        }
        catch
        {
            Write-Warning "Unable to tag S3 bucket $($bucketName): $($_.Exception.Message)"
        }

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