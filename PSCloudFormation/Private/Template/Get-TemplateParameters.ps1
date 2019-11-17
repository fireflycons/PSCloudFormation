function Get-TemplateParameters
{
    <#
    .SYNOPSIS
        Extract template parameter block as a PowerShell object graph.

    .PARAMETER TemplateResolver
        A resolver object returned by New-TemplateResolver.

    .OUTPUTS
        [object] Parameter block deserialised from JSON or YAML,
                    or nothing if template has no parameters.
    #>
    param
    (
        [object]$TemplateResolver
    )

    $template = $TemplateResolver.ReadTemplate()

    switch (Get-TemplateFormat -TemplateBody $template)
    {
        'JSON'
        {
            $templateObject = $template | ConvertFrom-Json

            if ($templateObject.PSObject.Properties.Name -contains 'Parameters')
            {
                return $templateObject.Parameters
            }
            else
            {
                # No parameters
                return
            }
        }

        'YAML'
        {
            $yaml = New-Object YamlDotNet.RepresentationModel.YamlStream
            $input = New-Object System.IO.StringReader($template)

            $yaml.Load($input)

            $root = [YamlDotNet.RepresentationModel.YamlMappingNode]$yaml.Documents[0].RootNode

            if ($null -eq $root)
            {
                throw "Empty document or not YAML"
            }

            $parametersKey = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("Parameters")

            if (-not $root.Children.ContainsKey($parametersKey))
            {
                # No parameters
                return
            }

            $parameters = [YamlDotNet.RepresentationModel.YamlMappingNode]$root.Children[$parametersKey]

            # Now create a PSObject that looks like parameters parsed from JSON
            $returnParameters = New-Object PSObject

            foreach ($parameterNode in $parameters.Children.GetEnumerator())
            {
                $parameterBody = $parameterNode.Value

                $parameterData = New-Object psobject

                foreach ($parameterPropertyNode in $parameterBody.Children.GetEnumerator())
                {
                    if ($parameterPropertyNode.Value -is [YamlDotNet.RepresentationModel.YamlScalarNode])
                    {
                        $parameterData | Add-Member -MemberType NoteProperty -Name $parameterPropertyNode.Key.ToString() -Value $parameterPropertyNode.Value.ToString()
                    }
                    elseif ($parameterPropertyNode.Value -is [YamlDotNet.RepresentationModel.YamlSequenceNode])
                    {
                        $values = @()
                        foreach ($seqNode in $parameterPropertyNode.Value.Children)
                        {
                            $values += $seqNode.Value.ToString()
                        }

                        $parameterData | Add-Member -MemberType NoteProperty -Name $parameterPropertyNode.Key.ToString() -Value $values
                    }
                    else
                    {
                        throw "Unexpected type $($parameterPropertyNode.Value.GetType().Name) in parameter block"
                    }
                }

                $returnParameters | Add-Member -MemberType NoteProperty -Name $parameterNode.Key.ToString() -Value $parameterData
            }

            return $returnParameters
        }
    }
}
