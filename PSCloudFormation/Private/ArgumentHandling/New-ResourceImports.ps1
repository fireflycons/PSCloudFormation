function New-ResourceImports
{
    # https://docs.aws.amazon.com/cli/latest/reference/cloudformation/create-change-set.html
    # Requires AWSSDK.CloudFormation v3.3.101 - which equates to AWSPowerShell >= 4.0.1.0

    param
    (
        [ValidateScript({ Test-Path -Path $_ -PathType Leaf})]
        [string]$ResourceFile
    )

    $imports = New-Object System.Collections.Generic.List[Amazon.CloudFormation.Model.ResourceToImport]

    switch ((Get-FileFormat -TemplateBody (Get-Content -Raw $ResourceFile)))
    {
        'JSON'
        {
            $resources = Get-Content -Path $ResourceFile -Raw | ConvertFrom-Json
            $resources |
            Foreach-Object {

                $import = New-Object Amazon.CloudFormation.Model.ResourceToImport -Property @{
                    LogicalResourceId = $_.LogicalResourceId
                    ResourceType = $_.ResourceType
                    ResourceIdentifier = New-Object 'System.Collections.Generic.Dictionary[String,String]'
                }

                $_.ResourceIdentifier.PSObject.Properties |
                Foreach-Object {
                    $import.ResourceIdentifier.Add($_.Name, $_.Value)
                }

                $imports.Add($import)
            }
        }

        'YAML'
        {
            try
            {
                $yaml = New-Object YamlDotNet.RepresentationModel.YamlStream
                $input = New-Object System.IO.StreamReader($ResourceFile)

                $yaml.Load($input)

                $resources = $yaml.Documents[0].RootNode

                if ($null -eq $resources)
                {
                    throw "Empty document or not YAML"
                }

                $resourceType = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("ResourceType")
                $logicalResourceId = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("LogicalResourceId")
                $resourceIdentifier = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("ResourceIdentifier")

                $resources |
                Foreach-Object {

                    $resource = $_

                    $import = New-Object Amazon.CloudFormation.Model.ResourceToImport -Property @{
                        LogicalResourceId = $_[$logicalResourceId].Value
                        ResourceType = $_[$resourceType].Value
                        ResourceIdentifier = New-Object 'System.Collections.Generic.Dictionary[String,String]'
                    }

                    $_[$resourceIdentifier] |
                    Foreach-Object {
                        $import.ResourceIdentifier.Add($_.Key.Value, $_.Value.Value)
                    }

                    $imports.Add($import)
                }
            }
            finally
            {
                if ($input)
                {
                    $input.Dispose()
                }
            }
        }
    }

    $imports.ToArray()
}