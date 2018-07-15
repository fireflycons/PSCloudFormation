function Reset-PSCFNStack
{
    <#
    .SYNOPSIS
        Delete and recreate an existing stack

    .DESCRIPTION
        Completely replace an existing stack

        DYNAMIC PARAMETERS

        Once the -TemplateLocation argument has been suppied on the command line
        the function reads the template and creates additional command line parameters
        for each of the entries found in the "Parameters" section of the template.
        These parameters are named as per each parameter in the template and defaults
        and validation rules created for them as defined by the template.

        Thus, if a template parameter has AllowedPattern and AllowedValues properties,
        the resultant function argument will permit TAB completion of the AllowedValues,
        assert that you have entered one of these, and for AllowedPattern, the function
        argument will assert the regular expression.

        Template parameters with no default will become mandatory parameters to this function.
        If you do not supply them, you will be prompted for them and the help text for the
        parameter will be taken from the Description property of the parameter.

    .PARAMETER StackName
        Name of the stack to replace.

    .PARAMETER TemplateLocation
        Location of the template.
        This may be
        - Path to a local file
        - s3:// URL pointing to template in a bucket
        - https:// URL pointing to template in a bucket

    .PARAMETER Capabilities
        If the stack requires IAM capabilities, TAB auctocompletes between the capability types.

    .PARAMETER Wait
        If set, wait for stack update to complete before returning.

    .INPUTS
        System.String
            You can pipe the name or ARN of the stack you are replacing to this function

    .OUTPUTS
        System.String
            ARN of the new stack

    .NOTES
        This cmdlet genenerates additional dynamic command line parameters for all parameters found in the Parameters block of the supplied CloudFormation template

    .EXAMPLE

        Reset-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        First deletes any existing stack of the same name or ARN, then creates a new stack from a local template file and waits for it to complete.
        This template would have 'VpcCidr' defined within its parameter block

    .EXAMPLE

        Reset-PSCFNStack -StackName MyStack -TemplateLocation https://s3-eu-west-1.amazonaws.com/mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        As per the first example, but with the template located in S3.

    .EXAMPLE

        Reset-PSCFNStack -StackName MyStack -TemplateLocation s3://mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.1.0.0/16

        As per the first example, but using an S3 URL.
        Caveat to this mechanism is that you must have a default region set in the curent shell. The bucket is assumed to be in this region and the stack will also be built in this region.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [string]$StackName,

        [Parameter(Mandatory = $true)]
        [string]$TemplateLocation,

        [ValidateSet('CAPABILITY_IAM', 'CAPABILITY_NAMED_IAM')]
        [string]$Capabilities,

        [switch]$Wait
    )

    DynamicParam
    {
        #Create the RuntimeDefinedParameterDictionary
        New-Object System.Management.Automation.RuntimeDefinedParameterDictionary |
            New-CredentialDynamicParameters |
            New-TemplateDynamicParameters -TemplateLocation $TemplateLocation -EnforceMandatory
    }

    begin
    {
        $stackParameters = Get-CommandLineStackParameters -CallerBoundParameters $PSBoundParameters
        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters
    }

    end
    {
        # Pass all Update-PSCFNStack parameters except 'Rebuild' to New-PSCFNStack
        # This will include any common credential and template parameters
        $createParameters = @{}
        $PSBoundParameters.Keys |
            Where-Object { $_ -ine 'Rebuild'} |
            ForEach-Object {

            $createParameters.Add($_, $PSBoundParameters[$_])
        }

        Remove-PSCFNStack -StackName $StackName -Wait @credentialArguments
        New-PSCFNStack @createParameters
    }
}
