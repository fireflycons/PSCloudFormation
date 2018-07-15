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
            return $type
        }
    }

    # No match - assume string
    return 'String'
}
