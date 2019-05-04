function New-TemplateDynamicParameters
{
    <#
    .SYNOPSIS
        Create PowerShell dynamic parameters from template parameters.

    .DESCRIPTION
        Loads/downloads the template and parses the template body to extract arguments.
        Turns these parameters into PowerShell dynamic parameters for the
        New-PSCFNStack and Update-PSCCFNtack CmdLets, also applying any
        constraints indicated by AllowedPattern or AllowedValues, and
        creating regex contraints to validate AWS types like AWS::EC2::Image::Id

    .PARAMETER Dictionary
        RuntimeDefinedParameterDictionary to add CF template parameters to.

    .PARAMETER TemplateLocation
        Location of the template. May be either
        - Path to local file
        - S3 URI (which is converted to HTTPS URI for the current region)
        - HTTP(S) Uri

    .PARAMETER ParameterFile
        If present and non-null, path to a JSON file containing a list of parameter structures

    .PARAMETER UsePreviousTemplate
        Reuse the existing template that is associated with the stack that you are updating. Conditional: You must specify only TemplateLocationL, or set the UsePreviousTemplate to true.

    .PARAMETER StackName
        Used if -UsePreviousTemplate is true

    .PARAMETER EnforceMandatory
        This will be set for New-PSCFNStack, as parameters with no defaults must be given a value
        For Update-PSCFNStack, it is not set as we will tell the stack to use previous values for any missing parameters

    .OUTPUTS
        [System.Management.Automation.RuntimeDefinedParameterDictionary]
        The dictionary that was passed in with new dynamic parameters to apply to caller added.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(ValueFromPipeline = $true)]
        [System.Management.Automation.RuntimeDefinedParameterDictionary]$Dictionary,
        [string]$TemplateLocation,
        [string]$ParameterFile,
        [bool]$UsePreviousTemplate,
        [string]$StackName,
        [switch]$EnforceMandatory
    )

    begin
    {
        # Assert only one of TemplateLocation or UsePreviousTemplate is passed
        if (-not ($PSBoundParameters.ContainsKey('TemplateLocation') -xor $PSBoundParameters.ContainsKey('UsePreviousTemplate')))
        {
            throw 'Must specify either TemplateLocation or UsePreviousTemplate, bot not both or neither.'
        }

        $templateArguments = @{}
        $PSBoundParameters.GetEnumerator() |
        Where-Object {
            ('TemplateLocation', 'StackName', 'UsePreviousTemplate') -icontains $_.Key
        } |
        ForEach-Object {
            $templateArguments.Add($_.Key, $_.Value)
        }

        # List of ParameterKey names from any supplied parameter file.
        # If we are importing parameters, then matching command line paramater must become optional
        $parameterFileParameters = @()

        if (-not [string]::IsNullOrEmpty($ParameterFile))
        {
            $parameterFileParameters = (Get-Content -Raw -Path $ParameterFile | ConvertFrom-Json).ParameterKey
        }
    }

    end
    {
        Initialize-RegionInfo

        (Get-TemplateParameters -TemplateResolver (New-TemplateResolver @templateArguments)).PSObject.Properties |
            ForEach-Object {

            $param = $_

            $paramDefinition = @{
                'Name'         = $param.Name
                'DPDictionary' = $Dictionary
            }

            if (-not $param.Value.PSObject.Properties['Type'])
            {
                # All template parameters require a Type property
                throw "Malformed parameter definition. Type is required"
            }

            $awsType = $param.Value.Type

            if ($Script:TemplateParameterValidators.ContainsKey($awsType))
            {
                # One of the defined AWS special parameter types
                $paramDefinition.Add('Type', 'String')
                $paramDefinition.Add('ValidatePattern', $Script:TemplateParameterValidators[$awstype])
            }
            elseif ($awsType -imatch 'List\<(?<ResourceId>[A-Z0-9\:]+)\>' -and $Script:TemplateParameterValidators.ContainsKey($Matches.ResourceId))
            {
                # List of one of the defined AWS special parameter types
                $paramDefinition.Add('Type', 'String[]')
                $paramDefinition.Add('ValidatePattern', $Script:TemplateParameterValidators[$Matches.ResourceId])
            }
            else
            {
                # Basic types with optional AllowedValues/AllowedPattern
                switch ($awsType)
                {
                    'Number'
                    {
                        $paramDefinition.Add('Type', 'Double')
                    }

                    'List<Number>'
                    {
                        $paramDefinition.Add('Type', 'Double[]')
                    }

                    'CommaDelimitedList'
                    {
                        $paramDefinition.Add('Type', 'String[]')
                    }

                    Default
                    {
                        $paramDefinition.Add('Type', 'String')
                    }
                }

                if ($param.Value.PSObject.Properties['AllowedValues'])
                {
                    $paramDefinition.Add('ValidateSet', $param.Value.AllowedValues);
                }

                if ($param.Value.PSObject.Properties['AllowedPattern'])
                {
                    $paramDefinition.Add('ValidatePattern', $param.Value.AllowedPattern);
                }
            }

            if ($param.Value.PSObject.Properties['Description'])
            {
                # Description becomes help test if the parameter is mandatory
                $paramDefinition.Add('HelpMessage', $param.Value.Description);
            }

            if ($EnforceMandatory -and -not $param.Value.PSObject.Properties['Default'] -and -not $parameterFileParameters -ccontains $param.Name)
            {
                # If no default in the template, parameter is mandatory
                $paramDefinition.Add('Mandatory', $true);
            }

            New-DynamicParam @paramDefinition
        }

        #return RuntimeDefinedParameterDictionary
        $Dictionary
    }
}
