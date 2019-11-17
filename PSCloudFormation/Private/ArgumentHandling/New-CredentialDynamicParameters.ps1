function New-CredentialDynamicParameters
{
    <#
        .SYNOPSIS
            Add the common credential and region parameters as dynamic parameters

        .PARAMETER Dictionary
            RuntimeDefinedParameterDictionary to add CF template parameters to.

        .OUTPUTS
            [System.Management.Automation.RuntimeDefinedParameterDictionary]
            The dictionary that was passed in with new dynamic parameters to apply to caller added.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(ValueFromPipeline = $true)]
        [System.Management.Automation.RuntimeDefinedParameterDictionary]$Dictionary
    )

    end
    {
        Initialize-RegionInfo

        $Script:CommonCredentialArguments.Keys |
            ForEach-Object {

                $validateSet = @{}

                if ($_ -ieq 'Region')
                {
                    $validateSet.Add('ValidateSet', $Script:RegionInfo.Keys)
                }

                New-DynamicParam -Name $_ -Type $Script:CommonCredentialArguments[$_]['Type'] -DPDictionary $Dictionary @validateSet -HelpMessage $Script:CommonCredentialArguments[$_]['Description']
            }

        $Dictionary
    }
}
