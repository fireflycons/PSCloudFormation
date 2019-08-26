function New-PSCFNPackage
{
<#
    .SYNOPSIS
        Create a deployment package a-la aws cloudformation package

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
        function Get-CfProperty
        {
        <#
            .SYNOPSIS
                Get a property reference to the property that may contain a path,
                such that we can modify object graph directly

            .PARAMETER PropertyName
                Property to find.
                May be proerty.property etc. in which case we walk the object graph recursively.

            .PARAMETER ResourceProperties
                Current point in resource properties object graph.

            .OUTPUTS
                [object]
                Reflected property object for modification
        #>
            param
            (
                [string]$PropertyName,
                [object]$ResourceProperties
            )

            $splitNames = $PropertyName -split '\.'
            $thisPropertyName = $splitNames | Select-Object -First 1
            $remainingPropertyNames = ($splitNames | Select-Object -Skip 1) -join '.'

            $thisProperty = $ResourceProperties.PSObject.Properties | Where-Object { $_.Name -eq $thisPropertyName }

            if ($null -eq $thisProperty)
            {
                # Didn't find it
                return $null
            }

            if (-not [string]::IsNullOrEmpty($remainingPropertyNames))
            {
                return Get-CfProperty -ResourceProperties $thisProperty.Value -PropertyName $remainingPropertyNames
            }

            return $thisProperty
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
    }

    end
    {
        try
        {
            # Get absolute path to template.
            $TemplateFile = (Resolve-Path -Path $TemplateFile).Path

            $template = (New-TemplateResolver -TemplateLocation $TemplateFile -CredentialArguments $credentialArguments).ReadTemplate()
            $templateObject = $null

            # Check YAML/JSON
            try
            {
                $templateObject = $template | ConvertFrom-Json
            }
            catch
            {
                Write-Warning 'Template cannot be parsed as JSON and YAML support unavailable until powershell-yaml supports AWS short-form intrinsics'
                Write-Warning 'Use cfn-flip to convert YAML to JSON first.'
                throw 'YAML templates not supported.'
                $templateObject = $null
            }

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

            $modifiedResources = 0

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
                        $propObject = Get-CfProperty -ResourceProperties $resource.Properties -PropertyName $propName

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
                        $OutputTemplateFile = Join-Pth [IO.Path]::GetDirectoryName($OutputTemplateFile) $outputTemplateFileName
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
        catch
        {
            Write-Host -ForegroundColor Red -BackgroundColor Black $_.Exception.Message
            throw "Error processing template '$TemplateFile'"
        }
    }
}