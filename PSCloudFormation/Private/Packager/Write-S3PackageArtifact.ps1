function Write-S3PackageArtifact
 {
<#
    .PARAMETER CredentialArguments
        Credential arguments passed to public function.

    .OUTPUTS
        [bool]
        $True if a file was uploaded; else $false

    .NOTES
        MD5 hash is compared with S3 object ETag value.
        This works for files up to 5GB, but then nobody would upload a 5GB lambda!
#>
    param
    (
        [string]$Path,
        [string]$Bucket,
        [string]$Key,
        [hashtable]$CredentialArguments,
        [hashtable]$Metadata,
        [string]$KmsKeyId,
        [switch]$Force

    )

    function Get-MD5Hash
    {
        param
        (
            [string]$Path
        )

        if ($null -ne (Get-Command -Name Get-FileHash -ErrorAction SilentlyContinue))
        {
            return (Get-FileHash -Path $Path -Algorithm MD5).Hash
        }

        [System.IO.Stream] $file = $null;
        [System.Security.Cryptography.MD5] $md5 = $null;
        try
        {
            $md5 = [System.Security.Cryptography.MD5]::Create()
            $file = [System.IO.File]::OpenRead($filePath)
            return [System.BitConverter]::ToString($md5.ComputeHash($file)).Replace('-', [string]::Empty)
        }
        finally
        {
            ($file, $md5) |
            Where-Object {
                $null -ne $_
            } |
            ForEach-Object {
                $_.Dispose()
            }
        }
    }

    # AWS quotes the ETag (MD5 hash value)
    $hash = '"' + (Get-MD5Hash $Path) + '"'

    # To support localstack testing, we have to fudge EndpointURL if present
    $s3Arguments = Update-EndpointValue -CredentialArguments $CredentialArguments -Service S3

    if (-not $Force)
    {
        try
        {
            $s3Object = Get-S3Object -BucketName $Bucket -Key $Key @s3Arguments

            if ($null -ne $s3Object -and $s3Object.ETag -ieq $hash)
            {
                # Hashes match - nothing to do
                return $false
            }
        }
        catch
        {
            # Not found or some other error - proceed to upload
        }
    }

    # Upload the file

    if (-not (Get-S3Bucket -BucketName $Bucket))
    {
        Write-Verbose "Creating bucket: $Bucket"
        New-S3Bucket -BucketName $Bucket @s3Arguments
    }

    $additionalArguments = @{}

    if ($Metadata -and $Metadata.Keys.Length -gt 0)
    {
        $additionalArguments.Add('Metadata', $Metadata)
    }

    if (-not [string]::IsNullOrEmpty($KmsKeyId))
    {
        $additionalArguments.Add('ServerSideEncryptionKeyManagementServiceKeyId ', $KmsKeyId)
    }

    Write-S3Object -BucketName $Bucket -Key $Key -File $Path @s3Arguments @additionalArguments | Out-Null
    return $true
}