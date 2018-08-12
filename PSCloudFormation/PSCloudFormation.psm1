#region Module Init

$ErrorActionPreference = 'Stop'

# Check for YAML support
if (Get-Module -ListAvailable | Where-Object {  $_.Name -ieq 'powershell-yaml' })
{
    Import-Module powershell-yaml
    $script:yamlSupport = $true
}
else
{
    Write-Warning 'YAML support unavailable'
    Write-Warning 'To enable, install powershell-yaml from the gallery'
    Write-Warning 'Install-Module -Name powershell-yaml'
    $Script:yamlSupport = $false
}

# Init region and AZ hash. AZ's are lazy-loaded when needed as this is time consuming
$script:RegionInfo = @{}

# Appveyor builds. There is no AWS environment defined so Get-EC2Region will fail
# Since the module is loaded and we need this data before there is a pester context, mock it manually
if (Get-PSCallStack | Where-Object { $_.Command -ieq 'Invoke-Pester' })
{
    $script:RegionInfo.Add('ap-northeast-1', $null)
    $script:RegionInfo.Add('ap-northeast-2', $null)
    $script:RegionInfo.Add('ap-south-1', $null)
    $script:RegionInfo.Add('ap-southeast-1', $null)
    $script:RegionInfo.Add('ap-southeast-2', $null)
    $script:RegionInfo.Add('ca-central-1', $null)
    $script:RegionInfo.Add('eu-central-1', $null)
    $script:RegionInfo.Add('eu-west-1', $null)
    $script:RegionInfo.Add('eu-west-2', $null)
    $script:RegionInfo.Add('eu-west-3', $null)
    $script:RegionInfo.Add('sa-east-1', $null)
    $script:RegionInfo.Add('us-east-1', $null)
    $script:RegionInfo.Add('us-east-2', $null)
    $script:RegionInfo.Add('us-west-1', $null)
    $script:RegionInfo.Add('us-west-2', $null)
}
else
{
    # Get-EC2Region asks an AWS service for current regions so is always up to date.
    # OTOH Get-AWSRegion is client-side and depends on the version of AWSPowerShell installed.
    Get-EC2Region |
    ForEach-Object {
        $script:RegionInfo.Add($_.RegionName, $null)
    }
}


# Hashtable of AWS custom parameter types vs. regexes to validate them
# Supporting 8 and 17 character identifiers
$Script:templateParameterValidators = @{

    'AWS::EC2::Image::Id'              = '^ami-([a-f0-9]{8}|[a-f0-9]{17})$'
    'AWS::EC2::Instance::Id'           = '^i-([a-f0-9]{8}|[a-f0-9]{17})$'
    'AWS::EC2::SecurityGroup::Id'      = '^sg-([a-f0-9]{8}|[a-f0-9]{17})$'
    'AWS::EC2::Subnet::Id'             = '^subnet-([a-f0-9]{8}|[a-f0-9]{17})$'
    'AWS::EC2::Volume::Id'             = '^vol-([a-f0-9]{8}|[a-f0-9]{17})$'
    'AWS::EC2::VPC::Id'                = '^vpc-([a-f0-9]{8}|[a-f0-9]{17})$'
    'AWS::EC2::AvailabilityZone::Name' = "^$(($script:RegionInfo.Keys | ForEach-Object {"($_)"}) -join '|' )[a-z]`$"
}

# Common Credential and Region Parameters and their types
$Script:commonCredentialArguments = @{

    AccessKey         = @{
        Type        = [string]
        Description = 'The AWS access key for the user account. This can be a temporary access key if the corresponding session token is supplied to the -SessionToken parameter.'
    }
    Credential        = @{
        Type        = [Amazon.Runtime.AWSCredentials]
        Description = 'An AWSCredentials object instance containing access and secret key information, and optionally a token for session-based credentials.'
    }
    ProfileLocation   = @{
        Type        = [string]
        Description = 'Used to specify the name and location of the ini-format credential file (shared with the AWS CLI and other AWS SDKs)'
    }
    ProfileName       = @{
        Type        = [string]
        Description = 'The user-defined name of an AWS credentials or SAML-based role profile containing credential information. The profile is expected to be found in the secure credential file shared with the AWS SDK for .NET and AWS Toolkit for Visual Studio. You can also specify the name of a profile stored in the .ini-format credential file used with the AWS CLI and other AWS SDKs.'
    }
    NetworkCredential = @{
        Type        = [System.Management.Automation.PSCredential]
        Description = "'Used with SAML-based authentication when ProfileName references a SAML role profile. Contains the network credentials to be supplied during authentication with the configured identity provider's endpoint. This parameter is not required if the user's default network identity can or should be used during authentication."
    }
    SecretKey         = @{
        Type        = [string]
        Description = 'The AWS secret key for the user account. This can be a temporary secret key if the corresponding session token is supplied to the -SessionToken parameter.'
    }
    SessionToken      = @{
        Type        = [string]
        Description = 'The session token if the access and secret keys are temporary session-based credentials.'
    }
    Region            = @{
        Type        = [string]
        Description = 'The system name of the AWS region in which the operation should be invoked. For example, us-east-1, eu-west-1 etc.'
    }
}

#endregion

# Get public and private function definition files.
$Public = @( Get-ChildItem -Path "$PSScriptRoot\Public\*.ps1" -ErrorAction SilentlyContinue )
$Private = @( Get-ChildItem -Path "$PSScriptRoot\Private\*.ps1" -ErrorAction SilentlyContinue )

# Dot source the files
foreach ($import in @($Public + $Private))
{
    try
    {
        . $import.FullName
    }
    catch
    {
        Write-Error -Message "Failed to import function $($import.FullName): $_"
    }
}

# Export public functions
Export-ModuleMember -Function $Public.Basename
