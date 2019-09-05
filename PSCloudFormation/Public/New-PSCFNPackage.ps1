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
                    $JsonProperties.PSObject.Properties | Where-Object { $_.Name -eq $thisPropertyName }

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
                        $retval = Get-ResourcePropertyNode -YamlProperties $YamlProperties.Children[$requiredKey] -PropertyName $remainingPropertyNames
                    }

                    # This mapping contains the value we need to change
                    $retval =  $YamlProperties
                }
            }

            Assert-True { $retval -is [YamlDotNet.RepresentationModel.YamlMappingNode] }
            return $retval
        }

        function Get-PathToReferencedFilesystemObject
        {
            param
            (
                [string]$ParentTemplate,
                [string]$ReferencedFileSystemObject
            )

            if ([IO.Path]::IsPathRooted($ReferencedFileSystemObject))
            {
                # Will do an existence check
                (Resolve-Path $ReferencedFileSystemObject).Path
            }
            else
            {
                # Work out path of object relative to current template
                (Resolve-Path -Path (Join-Path ([IO.Path]::GetDirectoryName($ParentTemplate)) $ReferencedFileSystemObject)).Path
            }
        }

        function New-S3ObjectUrl
        {
            param
            (
                [string]$Bucket,
                [string]$Prefix,
                [string]$Artifact
            )

            $filename = [IO.Path]::GetFileName($Artifact)

            if ([string]::IsNullOrEmpty($prefix))
            {
                "https://$($Bucket).s3.amazonaws.com/$($filename)"
            }
            else
            {
                "https://$($Bucket).s3.amazonaws.com/$($Prefix.TrimEnd('/', '\'))/$($filename)"
            }
        }

        function New-S3Bundle
        {
        <#
            .SYNOPSIS
                Creates a bunndle (S3Key/S3Bucket) object used by Lambda and Elastic Beanstalk
        #>
            param
            (
                [string]$Bucket,
                [string]$Prefix,
                [string]$ArtifactZip
            )

            $filename = [IO.Path]::GetFileName($ArtifactZip)

            New-Object PSObject -Property @{
                S3Bucket = $S3Bucket
                S3Key = $(
                    if (-not [string]::IsNullOrEmpty($S3Prefix))
                    {
                        $S3Prefix.TrimEnd('/', '\') + '/' + $filename
                    }
                    else
                    {
                        $filename
                    }
                )
            }
        }

        $credentialArguments = Get-CommonCredentialParameters -CallerBoundParameters $PSBoundParameters

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
            $cfnFlip = $(
                foreach ($exe in @('cfn-flip.exe', 'cfn-flip'))
                {
                    $cmd = Get-Command -Name $exe -ErrorAction SilentlyContinue

                    if ($null -ne $cmd)
                    {
                        $cmd
                        break;
                    }
                }
            )

            # Get absolute path to template.
            $TemplateFile = (Resolve-Path -Path $TemplateFile).Path

            $template = (New-TemplateResolver -TemplateLocation $TemplateFile -CredentialArguments $credentialArguments).ReadTemplate()
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

                                    $typeUsesBundle = ($resource.Type -eq 'AWS::Lambda::Function' -or $resource.Type -eq 'AWS::ElasticBeanstalk::ApplicationVersion')

                                    if (Test-Path -Path $referencedFileSystemObject -PathType Leaf)
                                    {
                                        if ($typeUsesBundle)
                                        {
                                            # All lambda deployments must be zipped
                                            $zipFile = [IO.Path]::GetFileNameWithoutExtension($referencedFileSystemObject) + ".zip"
                                            $propObject.Value = New-S3Bundle -Bucket $S3Bucket -Prefix $S3Prefix -ArtifactZip $zipFile
                                        }
                                        else
                                        {
                                            # Artifact is a single file - this will be uploaded directly.
                                            # Write-S3Object ...
                                            $propObject.Value = New-S3ObjectUrl -Bucket $S3Bucket -Prefix $S3Prefix -Artifact $referencedFileSystemObject
                                        }
                                    }
                                    else
                                    {
                                        # Artifact is a directory. Must be zipped.
                                        $zipFile = [IO.Path]::GetFileName($referencedFileSystemObject) + ".zip"
                                        $propObject.Value = $(
                                            if ($typeUsesBundle)
                                            {
                                                New-S3Bundle -Bucket $S3Bucket -Prefix $S3Prefix -ArtifactZip $zipFile
                                            }
                                            else
                                            {
                                                New-S3ObjectUrl -Bucket $S3Bucket -Prefix $S3Prefix -Artifact $zipFile
                                            }
                                        )
                                    }

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

                    $typeNode = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("Type")
                    $propertiesNode = New-Object YamlDotNet.RepresentationModel.YamlScalarNode("Properties")

                    foreach ($k in $resources.Children.Keys)
                    {
                        $resourceName = $k.Value

                        $resourceBody = [YamlDotNet.RepresentationModel.YamlMappingNode]($resources.Children[$k])
                        $type = [YamlDotNet.RepresentationModel.YamlScalarNode]$resourceBody.Children[$typeNode]
                        $properties = [YamlDotNet.RepresentationModel.YamlMappingNode]$resourceBody.Children[$propertiesNode]

                        if ($null -eq $properties -or $null -eq $type -or $resourceTransforms.Type -notcontains $type.Value)
                        {
                            continue
                        }

                        # Get type name
                        $type = $type.Value

                        # process types
                        $transform = $resourceTransforms |
                        Where-Object {
                            $_.Type -eq $type
                        }

                        $transform.Properties |
                        Foreach-Object {

                            $propName = $_
                            $propObject = Get-ResourcePropertyNode -YamlProperties $properties -PropertyName $propName

                            Assert-True { $propObject -is [YamlDotNet.RepresentationModel.YamlMappingNode] }

                            if ($null -ne $propObject)
                            {
                                $k = New-Object YamlDotNet.RepresentationModel.YamlScalarNode(($propName -split '\.') | Select-Object -Last 1)
                                $v = $propObject.Children[$k].value

                                if (Test-IsFileSystemPath -PropertyValue $v)
                                {
                                    $referencedFileSystemObject = Get-PathToReferencedFilesystemObject -ParentTemplate $TemplateFile -ReferencedFileSystemObject $propObject.Value

                                    if ($resource.Type -eq 'AWS::Cloudformation::Stack')
                                    {
                                        # Recurse
                                    }

                                    $typeUsesBundle = ($type -eq 'AWS::Lambda::Function' -or $type -eq 'AWS::ElasticBeanstalk::ApplicationVersion')

                                    if (Test-Path -Path $referencedFileSystemObject -PathType Leaf)
                                    {
                                        if ($typeUsesBundle)
                                        {
                                            # All lambda deployments must be zipped
                                            $zipFile = [IO.Path]::GetFileNameWithoutExtension($referencedFileSystemObject) + ".zip"
                                            $newValue = New-S3Bundle -Bucket $S3Bucket -Prefix $S3Prefix -ArtifactZip $zipFile
                                        }
                                        else
                                        {
                                            # Artifact is a single file - this will be uploaded directly.
                                            # Write-S3Object ...
                                            $newValue  = New-S3ObjectUrl -Bucket $S3Bucket -Prefix $S3Prefix -Artifact $referencedFileSystemObject
                                        }
                                    }
                                    else
                                    {
                                        # Artifact is a directory. Must be zipped.
                                        $zipFile = [IO.Path]::GetFileName($referencedFileSystemObject) + ".zip"
                                        $newValue  = $(
                                            if ($typeUsesBundle)
                                            {
                                                New-S3Bundle -Bucket $S3Bucket -Prefix $S3Prefix -ArtifactZip $zipFile
                                            }
                                            else
                                            {
                                                New-S3ObjectUrl -Bucket $S3Bucket -Prefix $S3Prefix -Artifact $zipFile
                                            }
                                        )
                                    }
                                }
                            }
                        }
                    }

                    $sw = New-Object System.IO.StringWriter
                    $yaml.Save($sw)

                    $template = $sw.ToString()
                    throw "DEBUG BREAK"
<#
        public static string UpdateCodeLocationInYamlTemplate(string templateBody, string s3Bucket, string s3Key)
        {
            var s3Url = $"s3://{s3Bucket}/{s3Key}";

            // Setup the input
            var input = new StringReader(templateBody);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // Examine the stream
            var root = (YamlMappingNode)yaml.Documents[0].RootNode;

            if (root == null)
                return templateBody;

            var resourcesKey = new YamlScalarNode("Resources");

            if (!root.Children.ContainsKey(resourcesKey))
                return templateBody;

            var resources = (YamlMappingNode)root.Children[resourcesKey];

            foreach (var resource in resources.Children)
            {
                var resourceBody = (YamlMappingNode)resource.Value;
                var type = (YamlScalarNode)resourceBody.Children[new YamlScalarNode("Type")];
                var properties = (YamlMappingNode)resourceBody.Children[new YamlScalarNode("Properties")];

                if (properties == null) continue;
                if (type == null) continue;

                if (string.Equals(type?.Value, "AWS::Serverless::Function", StringComparison.Ordinal))
                {
                    properties.Children.Remove(new YamlScalarNode("CodeUri"));
                    properties.Add("CodeUri", s3Url);
                }
                else if (string.Equals(type?.Value, "AWS::Lambda::Function", StringComparison.Ordinal))
                {
                    properties.Children.Remove(new YamlScalarNode("Code"));
                    var code = new YamlMappingNode();
                    code.Add("S3Bucket", s3Bucket);
                    code.Add("S3Key", s3Key);

                    properties.Add("Code", code);
                }
            }
            var myText = new StringWriter();
            yaml.Save(myText);

            return myText.ToString();
        }

#>
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