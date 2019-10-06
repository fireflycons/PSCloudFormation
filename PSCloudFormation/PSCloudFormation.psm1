#region Module Init

$ErrorActionPreference = 'Stop'

# Import assemblies
. (Join-Path $PSScriptRoot 'lib/Initialize-Assemblies.ps1')

# Get cfn-flip if present. That's the only safe way to convert short-form intrinsics
$script:cfnFlip = $(
    foreach ($exe in @('cfn-flip.exe', 'cfn-flip'))
    {
        $cmd = Get-Command -Name $exe -ErrorAction SilentlyContinue

        if ($null -ne $cmd)
        {
            $cmd
            break;
        }
    }
)

$script:haveCfnFlip = ($null -ne $script:cfnFlip)

if (-not $script:haveCfnFlip)
{
    Write-Warning "cfn-flip not found. The following will not work as indicated:"
    Write-Warning "- New-PSCFNPackage will not do any format conversion as is the behaviour of 'aws cloudformation package'. Output will be in same format as input."
    Write-Warning "See https://github.com/awslabs/aws-cfn-template-flip fo how to install."
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
    EndpointUrl       = @{
        Type        = [string]
        Description = 'The endpoint to make the call against.'
    }
}

$script:localStackPorts = @{
    S3 = 4572
    CF = 4581
}

#endregion

# Get public and private function definition files.
$Public = @( Get-ChildItem -Path ([IO.Path]::Combine($PSScriptRoot, "Public", "*.ps1")) -ErrorAction SilentlyContinue )
$Private = @( Get-ChildItem -Recurse -Path ([IO.Path]::Combine($PSScriptRoot, "Private", "*.ps1")) -ErrorAction SilentlyContinue )

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
