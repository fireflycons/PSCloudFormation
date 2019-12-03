function Get-ParameterTypeFromStringValue
{
    <#
    .SYNOPSIS
        Determine a parameter type from a sample value

    .PARAMETER Value
        Value to test

    .OUTPUTS
        AWS parameter type, or string if a type was not matched.
    #>
    param
    (
        [string]$Value
    )

    Initialize-RegionInfo

    foreach ($type in $Script:TemplateParameterValidators.Keys)
    {
        if ($Value -match $Script:TemplateParameterValidators[$type])
        {
            if ($type -ne 'AWS::EC2::AvailabilityZone::Name')
            {
                # All other types are exact match
                return $type
            }

            # Check it is a known AZ
            foreach($region in $Script:RegionInfo.Keys)
            {
                if ($Value -like "$($region)*")
                {
                    if ($null -eq $Script:RegionInfo[$region])
                    {
                        # Load AZs for region
                        $Script:RegionInfo[$region] = (Get-EC2AvailabilityZone -Region $region).ZoneName
                    }

                    if ($Script:RegionInfo[$region] -contains $Value)
                    {
                        # Value is a known AZ
                        return $type
                    }
                }
            }
        }
    }

    # No match - assume string
    return 'String'
}
