Add-Type -AssemblyName 'System.Linq'

$script:git = Get-Command -Name Git
$tf = 'terraform-provider-aws'
$tfSite = "https://github.com/hashicorp/$tf.git"
$tfClone = Join-Path $PSScriptRoot $tf
$awsSite = 'https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html'

$translations = @{
    CertificateManager = 'acm'
}

class Resource
{
    [string]$RealName
    [string]$Munged

    Resource($r, $m)
    {
        $this.RealName = $r
        $this.Munged = $m
    }
}

class MatchedResource
{
    [string]$TF
    [string]$AWS

    MatchedResource($t, $a)
    {
        $this.TF = $t
        $this.AWS = $a
    }
}

class TranslationRule
{
    [string]$Prefix
    [string]$Replacement
    [bool]$Exact

    TranslationRule($p, $r, $e)
    {
        $this.Prefix = $p
        $this.Replacement = $r
        $this.Exact = $e
    }

    [bool]IsMatch([string]$tfName)
    {
        if ($this.Exact)
        {
            return $this.Prefix.Equals($tfName)
        }

        return $tfName.StartsWith($this.Prefix)
    }

    [string]Replace([string]$tfName)
    {
        if ($this.Exact)
        {
            return $this.Replacement
        }

        return $tfName.Replace($this.Prefix, $this.Replacement)
    }
}

[System.Collections.Generic.List[TranslationRule]]$tr = @(
    [TranslationRule]::new('aws_instance', 'aws_ec2_instance', $true)
    [TranslationRule]::new('aws_eip', 'aws_ec2_eip', $true)
    [TranslationRule]::new('aws_key_pair', 'aws_ec2_keypair', $true)
    [TranslationRule]::new('aws_subnet', 'aws_ec2_subnet', $true)
    [TranslationRule]::new('aws_route', 'aws_ec2_route', $true)
    [TranslationRule]::new('aws_route_table_association', 'aws_ec2_subnetroutetableassociation', $true)
    [TranslationRule]::new('aws_vpc', 'aws_ec2_vpc', $true)
    [TranslationRule]::new('aws_elb', 'aws_elasticloadbalancing_loadbalancer', $true)
    [TranslationRule]::new('aws_lb', 'aws_elasticloadbalancingv2_loadbalancer', $true)
    [TranslationRule]::new('aws_route53_zone', 'aws_route53_hostedzone', $true)
    [TranslationRule]::new('aws_route53_record', 'aws_route53_recordset', $true)
    [TranslationRule]::new('aws_db_option_group', 'aws_rds_optiongroup', $true)
    [TranslationRule]::new('aws_cloudwatch_log_group', 'aws_log_loggroup', $true)
    [TranslationRule]::new('aws_cloudwatch_log_stream', 'aws_log_logstream', $true)
    [TranslationRule]::new('aws_redshift_cluster', 'aws_redshift_cluster', $true)
    [TranslationRule]::new('aws_autoscaling_group', 'aws_autoscaling_autoscalinggroup', $true)
    [TranslationRule]::new('aws_autoscaling_policy', 'aws_autoscaling_scalingpolicy', $true)
    [TranslationRule]::new('aws_autoscaling_schedule', 'aws_autoscaling_scheduledaction', $true)
    [TranslationRule]::new('aws_launch_configuration', 'aws_autoscaling_launchconfiguration', $true)
    [TranslationRule]::new('aws_security_group', 'aws_ec2_securitygroup', $true)
    [TranslationRule]::new('aws_rds_cluster', 'aws_rds_dbcluster', $false)
    [TranslationRule]::new('aws_redshift_', 'aws_redshift_cluster', $false)
    [TranslationRule]::new('aws_acm_', 'aws_certificatemanager_', $false)
    [TranslationRule]::new('aws_volume_', 'aws_ec2_volume_', $false)
    [TranslationRule]::new('aws_ebs_', 'aws_ec2_', $false)
    [TranslationRule]::new('aws_vpc_', 'aws_ec2_vpc_', $false)
    [TranslationRule]::new('aws_vpn_', 'aws_ec2_vpn_', $false)
    [TranslationRule]::new('aws_lb_', 'aws_elasticloadbalancingv2_', $false)
    [TranslationRule]::new('aws_route_table', 'aws_ec2_route_table', $false)
    [TranslationRule]::new('aws_db_cluster', 'aws_rds_dbcluster', $false)
    [TranslationRule]::new('aws_db_', 'aws_rds_db', $false)
    [TranslationRule]::new('aws_neptune_cluster_', 'aws_neptune_dbcluster_', $false)
    [TranslationRule]::new('aws_cloudwatch_log_', 'aws_logs_', $false)
    [TranslationRule]::new('aws_cloudwatch_event_', 'aws_events_', $false)
    [TranslationRule]::new('aws_schemas_', 'aws_eventschemas_', $false)
)
try
{
    $pp = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'

    if (-not (Test-Path -Path $tfClone -PathType Container))
    {
        & $git clone --depth 1 --no-checkout $tfSite $tfClone
    }

    Write-Host "Gathering Terraform resource names..."
    Push-Location $tfClone
    $counter = 0

    [Resource[]]$tfResources = & $git ls-tree --full-name --name-only -r HEAD |
    Select-String 'resource_aws_' |
    Where-Object { $_ -notlike '*_test.go' } |
    # Where-Object { $_ -like '*aws_acm*' -or $_ -like '*aws_vpc*' } |
    ForEach-Object {
        if (++$counter % 20 -eq 0)
        {
            Write-Host -NoNewline '.'
        }

        $r = [IO.Path]::GetFileNameWithoutExtension($_).Substring('resource_'.Length)
        $munged = $r

        $delegate = [Func[TranslationRule, bool]] { $args[0].IsMatch($r) }
        $rule = [Linq.Enumerable]::FirstOrDefault($tr, $delegate)

        if ($null -ne $rule)
        {
            $munged = $rule.Replace($r)

            if ($rule.Exact)
            {
                # Exact rule will only match once, so remove for perfomance
                [void]$tr.Remove($rule)
            }
        }

        [Resource]::new($r, $munged.Replace('_', ''))
    }
    Write-Host
    Pop-Location

    Write-Host "Gathering AWS resource names..."
    $counter = 0

    [Resource[]]$awsResources = $(
        (Invoke-WebRequest $awsSite).Links |
        Where-Object { $_.href -like './AWS_*' } |
        Select-Object -ExpandProperty href |
        Foreach-Object {
            if (++$counter % 5 -eq 0)
            {
                Write-Host -NoNewline '.'
            }
            $ub = [System.UriBuilder]::new($awsSite)
            $ub.Path = (Join-Path (Split-Path -Path $ub.Path -Parent) $_).Replace('\', '/')
            (Invoke-WebRequest $ub.ToString()).Links |
            Where-Object { $_.innerText -like 'AWS::*' } |
            Select-Object -ExpandProperty innerText
        }
        Write-Host
    ) |
    Sort-Object -Unique |
    ForEach-Object {
        [Resource]::new($_, $_.Replace('::', '').ToLower())
    }

    $keyDelegate = [System.Func[Resource, string]] { $args[0].Munged }
    $resultDelegate = [System.Func[Resource, Resource, MatchedResource]] { [MatchedResource]::new($args[0].RealName, $args[1].RealName) }

    Write-Host "Matching resouces..."
    $matched = [Linq.Enumerable]::ToList([Linq.Enumerable]::Join($tfResources, $awsResources, $keyDelegate, $keyDelegate, $resultDelegate))
    Write-Host "Writing C# resource..."
    $matched | ConvertTo-Json | Out-File -FilePath ../src/Firefly.PSCloudFormation/Resources/terraform-resource-map.json
}
finally
{
    $ProgressPreference = $pp
    if (Test-Path -Path $tfClone -PathType Container)
    {
        # Remove-Item $tfClone -Recurse -Force
    }
}

