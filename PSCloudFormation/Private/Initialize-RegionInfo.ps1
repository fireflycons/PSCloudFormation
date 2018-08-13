function Initialize-RegionInfo
{
    <#
    .SYNOPSIS
        Lazy initialize module-wide variables that depend on AWS regions
    #>

    if (-not $Script:RegionInfo)
    {
        $Script:RegionInfo = @{}
        
        # Get-EC2Region asks an AWS service for current regions so is always up to date.
        # OTOH Get-AWSRegion is client-side and depends on the version of AWSPowerShell installed.
        Get-EC2Region |
            ForEach-Object {
            $Script:RegionInfo.Add($_.RegionName, $null)
        }
    }

    if (-not $Script:TemplateParameterValidators)
    {
        # Hashtable of AWS custom parameter types vs. regexes to validate them
        # Supporting 8 and 17 character identifiers
        $Script:TemplateParameterValidators = @{

            'AWS::EC2::Image::Id'              = '^ami-([a-f0-9]{8}|[a-f0-9]{17})$'
            'AWS::EC2::Instance::Id'           = '^i-([a-f0-9]{8}|[a-f0-9]{17})$'
            'AWS::EC2::SecurityGroup::Id'      = '^sg-([a-f0-9]{8}|[a-f0-9]{17})$'
            'AWS::EC2::Subnet::Id'             = '^subnet-([a-f0-9]{8}|[a-f0-9]{17})$'
            'AWS::EC2::Volume::Id'             = '^vol-([a-f0-9]{8}|[a-f0-9]{17})$'
            'AWS::EC2::VPC::Id'                = '^vpc-([a-f0-9]{8}|[a-f0-9]{17})$'
            'AWS::EC2::AvailabilityZone::Name' = "^$(($Script:RegionInfo.Keys | ForEach-Object {"($_)"}) -join '|' )[a-z]`$"
        }
    }
}