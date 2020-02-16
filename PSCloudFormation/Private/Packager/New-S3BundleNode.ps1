function New-S3BundleNode
{
<#
    .SYNOPSIS
        Creates a bundle (S3Key/S3Bucket) object used by Lambda and Elastic Beanstalk
#>
    param
    (
        [string]$Bucket,
        [string]$Prefix,
        [string]$ArtifactZip,

        [ValidateSet('Standard', 'ServerlessFunction')]
        [string]$BundleType,

        [Parameter(ParameterSetName = 'json')]
        [switch]$Json,

        [Parameter(ParameterSetName = 'yaml')]
        [switch]$Yaml
    )

    $filename = [IO.Path]::GetFileName($ArtifactZip)

    $s3Key = $(
        if (-not [string]::IsNullOrEmpty($S3Prefix))
        {
            $S3Prefix.TrimEnd('/', '\') + '/' + $filename
        }
        else
        {
            $filename
        }
    )

    if ($Json)
    {
        switch ($BundleType)
        {
            'Standard'
            {
                [pscustomobject][ordered]@{
                    S3Bucket = $Bucket
                    S3Key = $s3Key
                }
            }

            'ServerlessFunction'
            {
                [pscustomobject][ordered]@{
                    Bucket = $Bucket
                    Key = $s3Key
                }
            }

            default
            {
                throw "Unknown S3 bundle type: $_"
            }
        }
    }
    else #YAML
    {
        # Create mapping node
        $node = New-Object YamlDotNet.RepresentationModel.YamlMappingNode

        switch ($BundleType)
        {
            'Standard'
            {
                $node.Add("S3Bucket", $Bucket)
                $node.Add("S3Key", $s3Key)
            }

            'ServerlessFunction'
            {
                $node.Add("Bucket", $Bucket)
                $node.Add("Key", $s3Key)
            }
        }

        New-Object PSObject -Property @{ MappingNode = $node }
    }
}

