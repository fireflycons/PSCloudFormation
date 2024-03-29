$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName 'System.Linq'

########################################################################################
#
# GO section
#
# Requires go >= 1.16.0 to be founf in the path.
#
# Pull latest terraform-provider-aws, replace its main.go with schema-loader.go
# and run it which will pull out the entire resource schema as JSON and a list
# of the terraform AWS resource names for the next section of this script.
#
########################################################################################
$go = Get-Command go -ErrorAction SilentlyContinue

if (-not $go)
{
    throw "Cannot find Go compiler - we are not ready to Go."
}

$git = Get-Command git -ErrorAction SilentlyContinue



# Find the latest release of the provider source on github and pull at that commit
$latestRelease = Invoke-RestMethod https://api.github.com/repos/hashicorp/terraform-provider-aws/releases/latest -Headers @{ Accept = 'application/vnd.github.v3+json' }
$cloneDir = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
$resourceDir = (Resolve-Path -Path ../src/Firefly.PSCloudFormation/Resources).Path

try
{

    New-Item -Path $cloneDir -ItemType Directory | Out-Null
    Push-Location $cloneDir

    Write-Host "Cloning terraform-provider-aws $($latestRelease.tag_name)..."
    & $git clone -q --depth 1 --branch $latestRelease.tag_name https://github.com/hashicorp/terraform-provider-aws.git

    Set-Location terraform-provider-aws

    Write-Host "Replacing 'main' package"
    Copy-Item (Join-Path $PSSCriptRoot schema-loader.go) ./main.go

    Write-Host "Building provider..."
    & $go mod tidy
    & $go build -v .

    Write-Host "Running export"
    & $go run . $resourceDir $PSScriptRoot
}
finally
{
    Pop-Location
    Remove-Item -Path $cloneDir -Recurse -Force
}

########################################################################################
#
# Resource matching sectiion
#
# Scrape all the AWS resource types from the documentation website.
# Match as many of these with the terraform equivalents and write a JSON file.
#
########################################################################################

$awsSite = 'https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html'

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
    [TranslationRule]::new('aws_cloudwatch_log_group', 'aws_logs_loggroup', $true)
    [TranslationRule]::new('aws_cloudwatch_log_stream', 'aws_logs_logstream', $true)
    [TranslationRule]::new('aws_redshift_cluster', 'aws_redshift_cluster', $true)
    [TranslationRule]::new('aws_autoscaling_group', 'aws_autoscaling_autoscalinggroup', $true)
    [TranslationRule]::new('aws_autoscaling_policy', 'aws_autoscaling_scalingpolicy', $true)
    [TranslationRule]::new('aws_autoscaling_schedule', 'aws_autoscaling_scheduledaction', $true)
    [TranslationRule]::new('aws_launch_configuration', 'aws_autoscaling_launchconfiguration', $true)
    [TranslationRule]::new('aws_security_group', 'aws_ec2_securitygroup', $true)
    [TranslationRule]::new('aws_internet_gateway', 'aws_ec2_internetgateway', $true)
    [TranslationRule]::new('aws_nat_gateway', 'aws_ec2_natgateway', $true)
    [TranslationRule]::new('aws_flow_log', 'aws_ec2_flowlog', $true)
    [TranslationRule]::new('aws_cloudwatch_log_group', 'aws_logs_loggroup', $true)
    [TranslationRule]::new('aws_cloudwatch_metric_alarm', 'aws_cloudwatch_alarm', $true)
    [TranslationRule]::new('aws_appautoscaling_target', 'aws_applicationautoscaling_scalabletarget', $true)
    [TranslationRule]::new('aws_appautoscaling_policy', 'aws_applicationautoscaling_scalingpolicy', $true)
    [TranslationRule]::new('aws_sns_topic_subscription', 'aws_sns_subscription', $true)
    [TranslationRule]::new('aws_launch_template', 'aws_ec2_launchtemplate', $true)
    [TranslationRule]::new('aws_network_acl', 'aws_ec2_networkacl', $true)
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
    [TranslationRule]::new('aws_cognito_user_group', 'aws_cognito_userpoolgroup', $true)
    [TranslationRule]::new('aws_cognito_identity_pool_roles_attachment', 'aws_cognito_identitypoolroleattachment', $true)
    [TranslationRule]::new('aws_iam_policy', 'aws_iam_managedpolicy', $true)
)
try
{
    $pp = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'

    Write-Host "Reading Terraform resource names..."
    $counter = 0

    [Resource[]]$tfResources = Get-Content (Join-Path $PSScriptRoot terraform-aws-resource-names.txt) |
    Where-Object { -not [string]::IsNullOrEmpty($_) } |
    Sort-Object |
    ForEach-Object {
        if (++$counter % 20 -eq 0)
        {
            Write-Host -NoNewline '.'
        }

        $r = $_
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
            Where-Object { $_.outerHTML -like '*>AWS::*' } |
            ForEach-Object {
                [xml]$x = $_.outerHTML
                $x.a.InnerText
            }
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
    Write-Host "Wrote $($matched.Count) mappings"
}
finally
{
    $ProgressPreference = $pp
}

