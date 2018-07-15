function New-PSCFNStack
{
    <#
    .SYNOPSIS
        Creates a stack.

    .DESCRIPTION
        Creates a stack.

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
        Name for the new stack.

    .PARAMETER TemplateLocation
        Location of the template.
        This may be
        - Path to a local file
        - s3:// URL pointing to template in a bucket
        - https:// URL pointing to template in a bucket

    .PARAMETER Capabilities
        If the stack requires IAM capabilities, TAB auctocompletes between the capability types.

    .PARAMETER Wait
        If set, wait for stack creation to complete before returning.

    .INPUTS
        System.String
            You can pipe the new stack name to this function

    .OUTPUTS
        System.String
            ARN of the new stack

    .NOTES
        This cmdlet genenerates additional dynamic command line parameters for all parameters found in the Parameters block of the supplied CloudFormation template

    .EXAMPLE

        New-PSCFNStack -StackName MyStack -TemplateLocation .\mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.0.0.0/16

        Creates a new stack from a local template file and waits for it to complete.
        This template would have 'VpcCidr' defined within its parameter block

    .EXAMPLE

        New-PSCFNStack -StackName MyStack -TemplateLocation https://s3-eu-west-1.amazonaws.com/mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.0.0.0/16

        As per the first example, but with the template located in S3.

    .EXAMPLE

        New-PSCFNStack -StackName MyStack -TemplateLocation s3://mybucket/mystack.json -Capabilities CAPABILITY_IAM -Wait -VpcCidr 10.0.0.0/16

        As per the first example, but using an S3 URL.
        Caveat to this mechanism is that you must have a default region set in the curent shell. The bucket is assumed to be in this region and the stack will also be built in this region.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
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
        if (Test-StackExists -StackName $StackName -CredentialArguments $credentialArguments)
        {
            throw "Stack already exists: $StackName"
        }

        $stackArgs = New-StackOperationArguments -StackName $StackName -TemplateLocation $TemplateLocation -Capabilities $Capabilities -StackParameters $stackParameters
        $arn = New-CFNStack @stackArgs @credentialArguments

        if ($Wait)
        {
            Write-Host "Waiting for creation to complete"

            $stack = Wait-CFNStack -StackName $arn -Timeout ([TimeSpan]::FromMinutes(60).TotalSeconds) -Status @('CREATE_COMPLETE', 'ROLLBACK_IN_PROGRESS') @credentialArguments

            if ($stack.StackStatus -like '*ROLLBACK*')
            {
                Write-Host -ForegroundColor Red -BackgroundColor Black "Create failed: $arn"
                Write-Host -ForegroundColor Red -BackgroundColor Black (Get-StackFailureEvents -StackName $arn -CredentialArguments $credentialArguments | Sort-Object -Descending Timestamp | Out-String)

                throw $stack.StackStatusReason
            }
        }

        # Emit ARN
        $arn
    }
}
