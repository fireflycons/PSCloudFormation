function New-S3BundleNode
{
<#
    .SYNOPSIS
        Creates a bunndle (S3Key/S3Bucket) object used by Lambda and Elastic Beanstalk
#>
    param
    (
        [string]$Bucket,
        [string]$Prefix,
        [string]$ArtifactZip,

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
        New-Object PSObject -Property @{
            S3Bucket = $Bucket
            S3Key = $s3Key
        }
    }
    else #YAML
    {
        # Create mapping node
        $node = New-Object YamlDotNet.RepresentationModel.YamlMappingNode
        $node.Add("S3Bucket", $Bucket)
        $node.Add("S3Key", $s3Key)

        New-Object PSObject -Property @{ MappingNode = $node }
    }
}

