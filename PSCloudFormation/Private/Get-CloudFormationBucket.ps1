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

    # Must be a better way than this?
    $defaultRegionsMap = @{
        APN1  = 'ap-northeast-1'
        APN2  = 'ap-northeast-2'
        APN3  = 'ap-northeast-3'
        APS1  = 'ap-southeast-1'
        APS2  = 'ap-southeast-2'
        APS3  = 'ap-southeast-3'
        CAN1  = 'ca-central-1'
        CN    = 'cn-north-1'
        CN1   = 'cn-north-1'
        CNW1  = 'cn-northwest-1'
        EU    = 'eu-west-1'
        EUC1  = 'eu-central-1'
        EUN1  = 'eu-north-1'
        EUW1  = 'eu-west-1'
        EUW2  = 'eu-west-2'
        EUW3  = 'eu-west-3'
        GOV   = 'us-gov-west-1'
        GOVE1 = 'us-gov-east-1'
        GOVW1 = 'us-gov-west-1'
        SFO   = 'us-west-1'
        US    = 'us-east-1'
        USE2  = 'us-east-2'
        USW1  = 'us-west-1'
        USW2  = 'us-west-1'
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