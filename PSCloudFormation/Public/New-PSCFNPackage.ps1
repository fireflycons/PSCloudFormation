function New-PSCFNPackage
{
<#
    .SYNOPSIS
        Create a deployment package a-la aws cloudformation package

    .PARAMETER TemplateFile
        The path where your AWS CloudFormation template is located.

    .PARAMETER S3Bucket
        The name of the S3 bucket where this command uploads the artifacts that are referenced in your template.

    .PARAMETER S3Prefix
        A prefix name that the command adds to the artifacts' name when it uploads them to the S3 bucket. The prefix name is a path name (folder name) for the S3 bucket.

    .PARAMETER KmsKeyId
        The ID of an AWS KMS key that the command uses to encrypt artifacts that are at rest in the S3 bucket.

    .PARAMETER OutputTemplateFile
        The path to the file where the command writes the output AWS CloudFormation template. If you don't specify a path, the command writes the template to the standard output.

    .PARAMETER UseJson
        Indicates whether to use JSON as the format for the output AWS CloudFormation template. YAML is used by default.

    .PARAMETER ForceUpload
        Indicates whether to override existing files in the S3 bucket. Specify this flag to upload artifacts even if they match existing artifacts in the S3 bucket.
        CAVEAT: MD5 checksums are used to compare the local and S3 versions of the artifact. If the artifact is a zip file, then it will almost certainly be
        uploaded every time as zip files contain datetimes (esp. last access time) and other file metadata that may change from subsequent invocations of zip on the local artifacts.

    .PARAMETER Metadata
        A map of metadata to attach to ALL the artifacts that are referenced in your template.

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

        function Switch-Template
        {
            param
            (
                [string]$Template,

                [ValidateSet("JSON", "YAML")]
                [string]$Format,

                [string]$TempFolder
            )

            if (-not $script:haveCfnFlip)
            {
                return $template
            }

            $inputFile = Join-Path $TempFolder ([Guid]::NewGuid())
            $outputFIle = Join-Path $TempFolder ([Guid]::NewGuid())

            $Template | Out-FileWithoutBOM -FilePath $inputFile

            if ($Format -ieq 'JSON')
            {
                & $script:cfnFlip -j $inputFile $outputFile
            }
            else
            {
                & $script:cfnFlip -y $inputFile $outputFile
            }

            if ($LASTEXITCODE -ne 0)
            {
                throw "Error running cfn-flip"
            }

            return (Get-Content -Raw $outputFile)
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
            # Create a temp folder for any work
            $tempFolder = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
            New-Item -Path $tempFolder -ItemType Directory | Out-Null

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
                                        $referencedFileSystemObject = Resolve-NestedStack -TempFolder $tempFolder -TemplateFile $referencedFileSystemObject -CallerBoundParameters $PSBoundParameters
                                    }

                                    $node = Write-Resource -TempFolder $tempFolder -Json -Payload $referencedFileSystemObject -ResourceType $resource.Type -Bucket $S3Bucket -Prefix $S3Prefix -Force:$ForceUpload -CredentialArguments $credentialParameters -Metadata $Metadata
                                    $propObject.Value = $node.Value

                                    $modifiedResources++
                                }
                            }
                        }
                        catch
                        {
                            Write-Host -ForegroundColor Red -BackgroundColor Black $_.Exception.Message
                            Write-Host -ForegroundColor Red -BackgroundColor Black $_.ScriptStackTrace
                            throw "Error processing resource '$resourceName' ($($resource.Type))"
                        }
                    }

                    if ($modifiedResources -gt 0)
                    {
                        $haveOutputFile = -not ([string]::IsNullOrEmpty($OutputTemplateFile))
                        $renderedTemplate = $templateObject | ConvertTo-Json -Depth 20 | Format-Json

                        if ($script:cfnFlip -and -not $UseJson -and (-not $haveOutputFile -or ($haveOutputFile -and [IO.Path]::GetExtension($OutputTemplateFile) -ieq '.yaml')))
                        {
                            # If we can flip template format and either no output file, not UseJson, or output file is yaml
                            $renderedTemplate = Switch-Template -Template $renderedTemplate -Format YAML -TempFolder $tempFolder
                        }

                        if ($haveOutputFile)
                        {
                            $renderedTemplate | Out-FileWithoutBOM -FilePath $OutputTemplateFile
                        }
                        else
                        {
                            $renderedTemplate
                        }
                    }
                    else
                    {
                        Write-Host "$TemplateFile - Unchanged"
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

                                    if ($type -eq 'AWS::Cloudformation::Stack')
                                    {
                                        # Recurse nested stack.
                                        $referencedFileSystemObject  = Resolve-NestedStack -TempFolder $tempFolder -TemplateFile $referencedFileSystemObject -CallerBoundParameters $PSBoundParameters
                                    }

                                    $node = Write-Resource -TempFolder $tempFolder -Yaml -Payload $referencedFileSystemObject -ResourceType $type -Bucket $S3Bucket -Prefix $S3Prefix -Force:$ForceUpload -CredentialArguments $credentialParameters -Metadata $Metadata
                                    $propObject.Children.Remove($k) | Out-Null
                                    $propObject.Add($k.Value, $node.Value)
                                    $modifiedResources++
                                }
                            }
                        }
                    }

                    if ($modifiedResources -gt 0)
                    {
                        $haveOutputFile = -not ([string]::IsNullOrEmpty($OutputTemplateFile))

                        $sw = New-Object System.IO.StringWriter
                        $yaml.Save($sw, $false)  # Do not assign anchors

                        # Render
                        $renderedTemplate = $sw.ToString()

                        if ($script:cfnFlip -and $UseJson)
                        {
                            # If we can flip template format and either no output file, not UseJson, or output file is yaml
                            $renderedTemplate = Switch-Template -Template $renderedTemplate -Format JSON -TempFolder $tempFolder
                        }

                        if ($haveOutputFile)
                        {
                            $renderedTemplate | Out-FileWithoutBOM -FilePath $OutputTemplateFile
                        }
                        else
                        {
                            $renderedTemplate
                        }
                    }
                    else
                    {
                        Write-Host "$TemplateFile - Unchanged"
                    }
                }
            }

        }
        catch
        {
            Write-Host -ForegroundColor Red -BackgroundColor Black $_.Exception.Message
            Write-Host -ForegroundColor Red -BackgroundColor Black $_.ScriptStackTrace
            throw "Error processing template '$TemplateFile'"
        }
        finally
        {
            if (Test-Path -Path $tempFolder -PathType Container)
            {
                Remove-Item $tempFolder -Recurse -Force
            }
        }
    }
}