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

        $location = Get-S3BucketLocation -BucketName $bucketName @s3Arguments | Select -ExpandProperty Value

        return New-Object psobject -Property @{
            BucketName = $bucketName
            BucketUrl  = [uri]"https://s3.$($location).amazonaws.com/$bucketName"
        }
    }

    # Try to create it
    $response = New-S3Bucket -BucketName $bucketName @s3Arguments

    if ($response)
    {
        Write-Host "Created S3 bucket $bucketName to store oversize templates."
        Write-CloudFormationBucketTagging -BucketName $bucketName -CredentialArguments $s3Arguments

        $location = Get-S3BucketLocation -BucketName $bucketName @s3Arguments | Select-Object -ExpandProperty Value

        if ([string]::IsNullOrEmpty($location))
        {
            $sb = New-Object System.Text.StringBuilder

            $sb.AppendLine("Unable to get location for bucket $bucketName").
                Append('  ') | Out-Null

            $s3Arguments.Keys |
            Foreach-Object {
                $sb.Append("-($_) ")

                if ($_ -ieq 'SecretKey')
                {
                    $sb.Append('**** ')
                }
                else
                {
                    $sb.Append("$($s3Arguments[$_]) ") | Out-Null
                }
            }

            $sb.AppendLine();

            throw $sb.ToString()
        }

        return New-Object psobject -Property @{
            BucketName = $bucketName
            BucketUrl  = [uri]"https://s3.$($location).amazonaws.com/$bucketName"
        }
    }

    throw "Unable to create S3 bucket $bucketName"
}