function Get-ResourcePropertyNode
{
<#
    .SYNOPSIS
        Get a property reference to the property that may contain a path,
        such that we can modify object graph directly

    .PARAMETER PropertyName
        Property to find.
        May be proerty.property etc. in which case we walk the object graph recursively.

    .PARAMETER JsonProperties
        Current point in resource properties object graph.

    .OUTPUTS
        [object]
        Reflected property object for modification
#>
    param
    (
        [string]$PropertyName,

        [Parameter(ParameterSetName = 'json')]
        [PSObject]$JsonProperties,

        [Parameter(ParameterSetName = 'yaml')]
        [YamlDotNet.RepresentationModel.YamlMappingNode]$YamlProperties
    )

    $splitNames = $PropertyName -split '\.'
    $thisPropertyName = $splitNames | Select-Object -First 1
    $remainingPropertyNames = ($splitNames | Select-Object -Skip 1) -join '.'

    $retval = $null

    switch ($PSCmdlet.ParameterSetName)
    {
        'json'
        {
            $thisProperty = $JsonProperties.PSObject.Properties | Where-Object { $_.Name -eq $thisPropertyName }

            if ($null -eq $thisProperty)
            {
                # Didn't find it
                return $null
            }

            if (-not [string]::IsNullOrEmpty($remainingPropertyNames))
            {
                return Get-ResourcePropertyNode -JsonProperties $thisProperty.Value -PropertyName $remainingPropertyNames
            }

            return $thisProperty
        }

        'yaml'
        {
            # Here we need to return the mapping node that directly contains the scalar node of the property we want
            $requiredKey = New-Object YamlDotNet.RepresentationModel.YamlScalarNode($thisPropertyName)

            if (-not $YamlProperties.Children.ContainsKey($requiredKey))
            {
                # Not found
                return $null
            }

            if (-not [string]::IsNullOrEmpty($remainingPropertyNames))
            {
                return Get-ResourcePropertyNode -YamlProperties ($YamlProperties.Children[$requiredKey]) -PropertyName $remainingPropertyNames
            }

            # This mapping contains the value we need to change
            # Some weird shit goes on when trying to return the mapping node directly
            # Just getting a key-value pair which is $YamlProperties[0], so wrap the return value
            return New-Object PSObject -Property @{ MappingNode = $YamlProperties }
        }
    }
}

