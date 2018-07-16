function New-TemplateResolver
{
    <#
    .SYNOPSIS
        Resolve template location from path/url given on command lines

    .DESCRIPTION
        Returns an object that has methods for retrieving the template body
        and the size of the template file such that it can be checked for size limitations

    .PARAMETER TemplateLocation
        Location of the template. May be either
        - Path to local file
        - S3 URI (which is converted to HTTPS URI for the current region)
          Note that this only works if a default region is set in the shell and
          you don't try to point to a different region with -Region
        - HTTP(S) Uri

    .OUTPUTS
        Custom Object.
    #>

    param
    (
        [string]$TemplateLocation
    )

    $resolver = New-Object PSObject -Property @{

        'IsFile'     = $null
        'BucketName' = $null
        'Key'        = $null
        'Path'       = $null
        'Url'        = $null
    } |
        Add-Member -PassThru -Name ReadTemplate -MemberType ScriptMethod -Value {

        # Reads the template contents from either S3 or file system as approriate.
        if ($this.Path)
        {
            Get-Content -Raw -Path $this.Path
        }
        elseif ($this.BucketName -and $this.Key)
        {
            $tmpFile = "$([Guid]::NewGuid().ToString()).tmp"

            try
            {
                Read-S3Object -BucketName $this.BucketName -Key $this.Key -File $tmpFile | Out-Null
                Get-Content -Raw -Path $tmpFile
            }
            finally
            {
                Remove-Item -Path $tmpFile
            }
        }
        else
        {
            throw "Template location undefined"
        }
    } |
        Add-Member -PassThru -Name Length -MemberType ScriptMethod -Value {

        # Gets the file szie of the template
        if ($this.Path)
        {
            (Get-ItemProperty -Name Length -Path $this.Path).Length
        }
        elseif ($this.BucketName -and $this.Key)
        {
            (Get-S3Object -BucketName $this.BucketName -Key $this.Key).Size
        }
        else
        {
            throw "Template location undefined"
        }
    }

    $u = $null

    if ([Uri]::TryCreate($TemplateLocation, 'Absolute', [ref]$u))
    {
        switch ($u.Scheme)
        {
            's3'
            {

                $r = Get-DefaultAWSRegion

                # Convert to full URL
                if (-not $r)
                {
                    throw "Cannot determine region. Please use Initialize-AWSDefaults or Set-DefaultAWSRegion"
                }

                $resolver.Url = [Uri]("https://s3-{0}.amazonaws.com/{1}{2}" -f $r.Region, $u.Authority, $u.LocalPath)
                $resolver.BucketName = $u.Authority
                $resolver.Key = $u.LocalPath
                $resolver.IsFile = $false
            }

            'file'
            {

                $resolver.Path = $TemplateLocation
                $resolver.IsFile = $true
            }

            { $_ -ieq 'http' -or $_ -ieq 'https' }
            {

                $resolver.Url = $u
                $resolver.BucketName = $u.Segments[1].Trim('/');
                $resolver.Key = $u.Segments[2..($u.Segments.Length - 1)] -join ''
                $resolver.IsFile = $false
            }

            default
            {

                throw "Unsupported URI: $($u.ToString())"
            }
        }
    }
    else
    {
        $resolver.Path = $TemplateLocation
        $resolver.IsFile = $true
    }

    $resolver
}