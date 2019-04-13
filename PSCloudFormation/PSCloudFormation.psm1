#region Module Init

$ErrorActionPreference = 'Stop'

# Check for YAML support
if ($PSVersionTable.ContainsKey('PSEdition') -and $PSVersionTable.PSEdition -eq 'Core')
{
    Write-Warning 'YAML support unavailable. No suitable YAML modules exist for .NET Core'
    Write-Warning 'Convert YAML templates to JSON with https://github.com/awslabs/aws-cfn-template-flip'
    $Script:yamlSupport = $false
}
else
{
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
}

# Init region and AZ hash. AZ's are lazy-loaded when needed as this is time consuming
$Script:RegionInfo = $null
$Script:TemplateParameterValidators = $null

# Common Credential and Region Parameters and their types
$Script:CommonCredentialArguments = @{

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
$Public = @( Get-ChildItem -Path ([IO.Path]::Combine($PSScriptRoot, "Public", "*.ps1")) -ErrorAction SilentlyContinue )
$Private = @( Get-ChildItem -Path ([IO.Path]::Combine($PSScriptRoot, "Private", "*.ps1")) -ErrorAction SilentlyContinue )

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
