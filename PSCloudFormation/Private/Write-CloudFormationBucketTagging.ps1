function Write-CloudFormationBucketTagging
{
<#
    .SYNOPSIS
        Write cloudformation bucket tags if not present

    .PARAMETER BucketName
        Bucket to tag

    .PARAMETER CredentialArguments
        Common credential arguments - assmued to be S3 endpoint adjusted
#>
    param
    (
        [string]$BucketName,
        [hashtable]$CredentialArguments
    )

    if ((Get-S3BucketTagging -BucketName $BucketName @CredentialArguments | Measure-Object).Count -eq 0)
    {
        try
        {
            $module = (Get-Command (Get-PSCallStack | Select-Object -First 1).Command).Module
            Write-S3BucketTagging -BucketName $BucketName @CredentialArguments -TagSet @(
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
    }
}