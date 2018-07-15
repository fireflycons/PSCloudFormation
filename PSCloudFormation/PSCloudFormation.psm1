#region Module Init

$ErrorActionPreference = 'Stop'

# Hashtable of AWS custom parameter types vs. regexes to validate them
# Supporting 8 and 17 character identifiers
# AWS::EC2::AvailabilityZone::Name is handled separately by querying AWSPowerShell for AZs valid for the region we are executing in.
$Script:templateParameterValidators = @{

    'AWS::EC2::Image::Id'              = '^\s*ami-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Instance::Id'           = '^\s*i-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::SecurityGroup::Id'      = '^\s*sg-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Subnet::Id'             = '^\s*subnet-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Volume::Id'             = '^\s*vol-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::VPC::Id'                = '^\s*vpc-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::AvailabilityZone::Name' = '^\s*[a-z]{2}-[a-z]+-\d[a-z]\s*$'
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
