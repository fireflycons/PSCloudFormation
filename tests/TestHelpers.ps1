function Get-TestRegionList
{
    $isolatedRegions = @(
        'us-iso-east-1'
        'us-isob-east-1'
    )

    $regions = Get-AWSRegion | Select-Object -ExpandProperty Region

    # Remove isolated regions
    $compareResult = Compare-Object -ReferenceObject $regions -DifferenceObject $isolatedRegions -IncludeEqual

    if ($compareResult | Where-Object { $_.SideIndicator -eq '=='})
    {
        Write-Warning "Isolated regions ($($isolatedRegions -join ',')) will be ignored for purpose of these tests"
        $regions = $compareResult | Where-Object { $_.SideIndicator -eq '<=' } | Select-Object -ExpandProperty InputObject
    }

    $regions
}