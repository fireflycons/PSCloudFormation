function Get-PSCFNStackOutputs
{
    <#
    .SYNOPSIS
        Get the outputs of a stack in various formats

    .DESCRIPTION
        This function can be used to assist creation of new CloudFormation templates
        that refer to the outputs of another stack.

        It can be used to generate either mapping or prarameter blocks based on these outputs
        by converting the returned object to JSON or YAML

    .PARAMETER StackName
        One or more stacks to process. One object is produced for each stack

    .PARAMETER AsMappingBlock
        If set (default), returned object is formatted as a CloudFomration mapping block.
        Converting the output to JSON or YAML renders text that can be pasted within a Mappings declararion.

    .PARAMETER AsParameterBlock
        If set, returned object is formatted as a CloudFormation parameter block.
        Converting the output to JSON or YAML renders text that can be pasted within a Parameters declararion.

    .PARAMETER AsCrossStackReferences
        If set, returned object is formatted as a set of Fn::ImportValue statements, with any text matching the
        stack name within the output's ExportName being replaced with a placeholder generated from the stack name with the word 'Stack' appended.
        Make this a parameter to your new stack.

        Whilst the result converted to JSON is not much use as it is, the individual elements can
        be copied and pasted in where an Fn::ImportValue for that parameter would be used.

        YAML is not currently supported for this operation.

    .INPUTS
        [System.String[]] - You can pipe stack names or ARNs to this function

    .OUTPUTS
        [PSObject] - An object dependent on the setting of the above switches. Pipe the output to ConvertTo-Json or ConvertTo-Yaml

    .EXAMPLE

       Get-PSCFNStackOutputs -StackName MyStack -AsMappingBlock

       When converted to JSON or YAML, can be pasted into the Mapping declaration of another template

    .EXAMPLE

       Get-PSCFNStackOutputs -StackName MyStack -AsParameterBlock

       When converted to JSON or YAML, can be pasted into the Parameters declaration of another template

    .EXAMPLE

       Get-PSCFNStackOutputs -StackName MyStack -AsCrossStackReferences

       When converted to JSON or YAML, provides a collection of Fn::Import stanzas that can be individually pasted into a new template
    #>
    [CmdletBinding(DefaultParameterSetName = 'Mappings')]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [string[]]$StackName,

        [Parameter(ParameterSetName = 'Mappings')]
        [switch]$AsMappingBlock,

        [Parameter(ParameterSetName = 'Parameters')]
        [switch]$AsParameterBlock,

        [Parameter(ParameterSetName = 'Exports')]
        [switch]$AsCrossStackReferences
    )

    DynamicParam
    {
        #Create the RuntimeDefinedParameterDictionary
        New-Object System.Management.Automation.RuntimeDefinedParameterDictionary |
            New-CredentialDynamicParameters
    }

    begin
    {
        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters
    }

    process
    {
        $StackName |
            ForEach-Object {

            if (Test-StackExists -StackName $_ -CredentialArguments $credentialArguments)
            {
                $ti = New-Object System.Globalization.CultureInfo ("en-US")

                $stackParam = ($_.Split(('_', '-')) |
                Foreach-Object {
                    $ti.TextInfo.ToTitleCase($_)
                }) -join [string]::Empty

                $outputs = @{}
                $stack = Get-CFNStack -StackName $_ @credentialArguments
                $stack.Outputs |
                    ForEach-Object {

                    if ($AsParameterBlock)
                    {
                        $param = @{
                            Type    = Get-ParameterTypeFromStringValue -Value $_.OutputValue
                            Default = $_.OutputValue
                        }

                        if (-not [string]::IsNullOrEmpty($_.Description))
                        {
                            $param.Add('Description', $_.Description)
                        }

                        $outputs.Add($_.OutputKey, $param)
                    }
                    elseif ($AsCrossStackReferences)
                    {
                        if (-not [string]::IsNullOrEmpty($_.ExportName))
                        {
                            $param = @{
                                'Fn::ImportValue' = @{
                                    'Fn::Sub' = $_.ExportName.Replace($stack.StackName, "`${$($stackparam)Stack}")
                                }
                            }

                            $outputs.Add($_.OutputKey, $param)
                        }
                    }
                    else
                    {
                        $outputs.Add($_.OutputKey, $_.OutputValue)
                    }
                }

                if ($outputs.Count -gt 0)
                {
                    # Emit outputs as object
                    New-Object PSObject -Property $outputs
                }
            }
        }
    }
}
