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

    foreach ($type in $Script:templateParameterValidators.Keys)
    {
        if ($Value -match $Script:templateParameterValidators[$type])
        {
            if ($type -ne 'AWS::EC2::AvailabilityZone::Name')
            {
                # All other types are exact match
                return $type
            }

            # Check it is a known AZ
            foreach($region in $script:RegionInfo.Keys)
            {
                if ($Value -like "$($region)*")
                {
                    if ($null -eq $script:RegionInfo[$region])
                    {
                        # Load AZs for region
                        $script:RegionInfo[$region] = (Get-EC2AvailabilityZone -Region $region).ZoneName
                    }

                    if ($script:RegionInfo[$region] -contains $Value)
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
