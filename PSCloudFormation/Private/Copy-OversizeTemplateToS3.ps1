function Copy-OversizeTemplateToS3
{
<#
    .SYNOPSIS
        Copy filesystem templates that are too large to S3 for processing

    .DESCRIPTION
        Examine the user's arguments. If they point to a filesystem template
        that is larger than 51200 bytes, then  upload it to S3 and adjust
        the argumnents that will be passed to New-CFNStack/Update-CFNStack

    .PARAMETER CredentialArguments
        Credential arguments passed to public function.

    .PARAMETER StackArguments
        Stack arguments passed to public function.

#>
    param
    (
        [hashtable]$CredentialArguments,
        [hashtable]$StackArguments,
        [string]$TemplateLocation
    )

    $dateStamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')

    if (-not $StackArguments.ContainsKey('TemplateBody'))
    {
        return
    }

    # Measure the body size in bytes
    if ([System.Text.ASCIIEncoding]::ASCII.GetByteCount($StackArguments['TemplateBody']) -lt 51200)
    {
        return
    }

    # Oversize - need to upload
    $bucket = Get-CloudFormationBucket -CredentialArguments $CredentialArguments
    $key = $dateStamp + '-' + [IO.Path]::GetFileName($TemplateLocation)

    $ub = New-Object UriBuilder -ArgumentList $bucket.BucketUrl
    $ub.Path += "/$key"

    Write-Host "Copying oversize template to $($ub.Uri.ToString())"

    Write-S3Object -BucketName $bucket.BucketName -Key $key -File (Resolve-Path $TemplateLocation).Path @CredentialArguments

    # Now adjust the stack arguments to point to what we have just uploaded.

    $StackArguments.Remove('TemplateBody')
    $StackArguments.Add('TemplateURL', $ub.Uri.ToString())
}