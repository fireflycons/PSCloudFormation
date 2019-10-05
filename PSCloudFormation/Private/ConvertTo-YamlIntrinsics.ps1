function ConvertTo-YamlInstrinsics
{
    [CmdletBinding()]
    param
    (
        [Paramater(ValueFromPipeline = $true)]
        [PSCustomObject]$Template
    )

    begin
    {
        $script:intrinsics = @(
            'Fn::Base64'
            'Fn::Cidr'
            'Fn::FindInMap'
            'Fn::GetAtt'
            'Fn::GetAZs'
            'Fn::ImportValue'
            'Fn::Join'
            'Fn::Select'
            'Fn::Split'
            'Fn::Transform'
            'Ref'
            'Fn::And'
            'Fn::Equals'
            'Fn::Not'
            'Fn::Or'
        )

        function Get-ObjectMembers
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory=$True, ValueFromPipeline=$True)]
                [PSCustomObject]$obj
            )

            foreach ($prop in $obj.PSBoject.Properties)
            {
                if ($prop.TypeNameOfValue -eq 'System.Management.Automation.PSCustomObject')
                {
                    $prop.Value | Get-ObjectMembers | Out-Null
                }

                if ($script:intrinsics -contains $prop.Name)
                {
                    $yamlIntrinsic = '!' + $prop.Name.Substring($prop.Name.LastIndexOf(':') + 1)

                }
            }
        }
    }

    end
    {
        if (-not $Script:yamlSupport)
        {
            # If no YAML support, return template unchanged
            return $Template
        }
    }
}