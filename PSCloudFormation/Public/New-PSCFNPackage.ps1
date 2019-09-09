function New-PSCFNPackage
{
<#
    .SYNOPSIS
        Create a deployment package a-la aws cloudformation package

    .NOTES
        https://github.com/aws/aws-extensions-for-dotnet-cli/blob/master/src/Amazon.Lambda.Tools/LambdaUtilities.cs
#>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [ValidateScript( { Test-Path -Path $_ -PathType Leaf })]
        [string]$TemplateFile,

        [Parameter(Mandatory)]
        [string]$S3Bucket,

        [string]$S3Prefix,

        [string]$KmsKeyId,

        [string]$OutputTemplateFile,

        [Switch]$UseJson,

        [switch]$ForceUpload,

        [hashtable]$Metadata
    )

    DynamicParam
    {
        #Create the RuntimeDefinedParameterDictionary
        New-Object System.Management.Automation.RuntimeDefinedParameterDictionary |
            New-CredentialDynamicParameters
    }

    begin
    {
        function Assert-True
        {
            param
            (
                [scriptblock]$Predicate
            )

            if ((Invoke-Command -ScriptBlock $Predicate) -ne $true)
            {
                throw "Assertion failure: { $($Predicate.ToString()) }"
            }
        }

        $credentialParameters = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters

        $resourceTransforms = @(
            New-Object PSObject -Property @{
                Type = 'AWS::ApiGateway::RestApi'
                Properties = @('BodyS3Location')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::Lambda::Function'
                Properties = @('Code')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::Serverless::Function'
                Properties = @('CodeUri')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::AppSync::GraphQLSchema'
                Properties = @('DefinitionS3Location')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::AppSync::Resolver'
                Properties = @('RequestMappingTemplateS3Location', 'ResponseMappingTemplateS3Location')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::Serverless::Api'
                Properties = @('DefinitionUri')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::Include'
                Properties = @('Location')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::ElasticBeanstalk::ApplicationVersion'
                Properties = @('SourceBundle')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::CloudFormation::Stack'
                Properties = @('TemplateURL')
            }
            New-Object PSObject -Property @{
                Type = 'AWS::Glue::Job'
                Properties = @('Command.ScriptLocation')
            }
        )
    }

    end
    {
        try
        {
            # Get absolute path to template.
            $TemplateFile = (Resolve-Path -Path $TemplateFile).Path

            $template = (New-TemplateResolver -TemplateLocation $TemplateFile -credentialParameters $credentialParameters).ReadTemplate()
            $templateObject = $null
            $templateFormat = Get-TemplateFormat -TemplateBody $template

            $modifiedResources = 0

            switch ($templateFormat)
            {
                'JSON'
                {
                    $templateObject = $template | ConvertFrom-Json

                    $templateObject.Resources.PSObject.Properties |
                    Where-Object {
                        $resourceTransforms.Type -contains $_.Value.Type
                    } |
                    ForEach-Object {

                        $resource = $_.Value
                        $resourceName = $_.Name

                        try
                        {
                            $transform = $resourceTransforms |
                            Where-Object {
                                $_.Type -eq $resource.Type
                            }

                            $transform.Properties |
                            Foreach-Object {

                                $propName = $_
                                $propObject = Get-ResourcePropertyNode -JsonProperties $resource.Properties -PropertyName $propName

                                if ($null -ne $propObject -and (Test-IsFileSystemPath -PropertyValue $propObject.Value))
                                {
                                    $referencedFileSystemObject = Get-PathToReferencedFilesystemObject -ParentTemplate $TemplateFile -ReferencedFileSystemObject $propObject.Value

                                    if ($resource.Type -eq 'AWS::Cloudformation::Stack')
                                    {
                                        # Recurse nested stack.
                                        # Create name for modified nested template
                                        $ext = $(
                                            if ($UseJson)
                                            {
                                                '.json'
                                            }
                                            else
                                            {
                                                '.yaml'
                                            }
                                        )

                                        $nestedOutputTemplateFile = Join-Path ([IO.Path]::GetDirectoryName($referencedFileSystemObject)) ([IO.Path]::GetFileNameWithoutExtension($referencedFileSystemObject) + "-packaged" + $ext)

                                        $argumentHash = @{}

                                        $PSBoundParameters.Keys |
                                        Where-Object {
                                            ('OutputTemplateFile', 'TemplateFile') -inotcontains $_
                                        } |
                                        ForEach-Object {
                                            $argumentHash.Add($_, $PSBoundParameters[$_])
                                        }

                                        $argumentHash.Add('TemplateFile', $referencedFileSystemObject)
                                        $argumentHash.Add('OutputTemplateFile', $nestedOutputTemplateFile)

                                        New-PSCFNPackage @argumentHash

                                        if (Test-Path -Path $nestedOutputTemplateFile)
                                        {
                                            # Substitutions were made
                                            $referencedFileSystemObject = $nestedOutputTemplateFile
                                        }
                                    }

                                    $node = Write-Resource -Json -Payload $referencedFileSystemObject -ResourceType $resource.Type -Bucket $S3Bucket -Prefix $S3Prefix -Force:$ForceUpload -CredentialArguments $credentialParameters
                                    $propObject.Value = $node.Value

                                    $modifiedResources++
                                }
                            }
                        }
                        catch
                        {
                            Write-Host -ForegroundColor Red -BackgroundColor Black $_.Exception.Message
                            throw "Error processing resource '$resourceName' ($($resource.Type))"
                        }
                    }

                    if ($modifiedResources -gt 0)
                    {
<#                        
                        $forceJson = (-not $Script:yamlSupport -and -not $UseJson)

                        if ($forceJson)
                        {
                            Write-Warning "YAML support unavailable. Output will be JSON"

                            if (-not ([string]::IsNullOrEmpty($OutputTemplateFile)))
                            {
                                $outputTemplateFileName = [IO.Path]::GetFileNameWithoutExtension($OutputTemplateFile) + ".json"
                                $OutputTemplateFile = Join-Path [IO.Path]::GetDirectoryName($OutputTemplateFile) $outputTemplateFileName
                            }
                        }

                        $renderedTemplate = $(
                            if ($UseJson -or $forceJson)
                            {
                                $templateObject | ConvertTo-Json -Depth 20
                            }
                            else
                            {
                                $templateObject | ConvertTo-Yaml
                            }
                        )
#>
                        $renderedTemplate = $templateObject | ConvertTo-Json -Depth 20 | Format-Json

                        if ((-not ([string]::IsNullOrEmpty($OutputTemplateFile))))
                        {
                            $renderedTemplate | Out-File -FilePath $OutputTemplateFile
                        }
                        else
                        {
                            $renderedTemplate
                        }
                    }
                }

                'YAML'
                {
                    if (-not $script:yamlSupport)
                    {
                        throw "YAML support unavailable."
                    }

                    # Do using raw yaml stream so as not to bollox any short form intrinsics
                    # https://github.com/aws/aws-extensions-for-dotnet-cli/blob/master/src/Amazon.Lambda.Tools/LambdaUtilities.cs line 283

                    $yaml = New-Object YamlDotNet.RepresentationModel.YamlStream
                    $input = New-Object System.IO.StringReader($template)

                    $yaml.Load($input)

                    $root = [YamlDotNet.RepresentationModel.YamlMappingNode]$yaml.Documents[0].RootNode

                    if ($null -eq $root)
                    {
                        throw "Empty document or not YAML"
                    }

                    $resourcesKey = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("Resources")

                    if (-not $root.Children.ContainsKey($resourcesKey))
                    {
                        # TODO return doc
                    }

                    $resources = [YamlDotNet.RepresentationModel.YamlMappingNode]$root.Children[$resourcesKey]

                    $typeSelector = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("Type")
                    $propertiesSelector = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("Properties")

                    foreach ($resourceNode in $resources.Children.GetEnumerator())
                    {
                        $resourceName = $resourceNode.Key.ToString()
                        $resourceBody = $resourceNode.Value
                        $typeNode = $resourceBody.Children[$typeSelector]
                        $propertiesNode = $resourceBody.Children[$propertiesSelector]

                        if ($null -eq $propertiesNode -or $null -eq $typeNode -or $resourceTransforms.Type -notcontains $typeNode.Value)
                        {
                            continue
                        }

                        # Get type name
                        $type = $typeNode.Value

                        # process types
                        $transform = $resourceTransforms |
                        Where-Object {
                            $_.Type -eq $type
                        }

                        $transform.Properties |
                        Foreach-Object {

                            $propName = $_
                            $propObject = Get-ResourcePropertyNode -YamlProperties $propertiesNode -PropertyName $propName

                            Assert-True { $propObject.MappingNode -is [YamlDotNet.RepresentationModel.YamlMappingNode] }

                            if ($null -ne $propObject)
                            {
                                $propObject = $propObject.MappingNode
                                $k = New-Object YamlDotNet.RepresentationModel.YamlScalarNode(($propName -split '\.') | Select-Object -Last 1)
                                $v = $propObject.Children[$k].value

                                if (Test-IsFileSystemPath -PropertyValue $v)
                                {
                                    $referencedFileSystemObject = Get-PathToReferencedFilesystemObject -ParentTemplate $TemplateFile -ReferencedFileSystemObject $v

                                    if ($resource.Type -eq 'AWS::Cloudformation::Stack')
                                    {
                                        # Recurse
                                    }

                                    $node = Write-Resource -Yaml -Payload $referencedFileSystemObject -ResourceType $type -Bucket $S3Bucket -Prefix $S3Prefix -Force:$ForceUpload -CredentialArguments $credentialParameters
                                    $propObject.Children.Remove($k) | Out-Null
                                    $propObject.Add($k.Value, $node.Value)
                                    $modifiedResources++
                                }
                            }
                        }
                    }

                    $sw = New-Object System.IO.StringWriter
                    $yaml.Save($sw, $false)  # Do not assign anchors

                    $template = $sw.ToString()
                    $template
                }
            }

        }
        catch
        {
            Write-Host -ForegroundColor Red -BackgroundColor Black $_.Exception.Message
            Write-Host -ForegroundColor Red -BackgroundColor Black $_.ScriptStackTrace
            throw "Error processing template '$TemplateFile'"
        }
    }
}