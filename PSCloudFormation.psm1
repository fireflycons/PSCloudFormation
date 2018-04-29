#region Module Init

$ErrorActionPreference = 'Stop'

# Hashtable of AWS custom parameter types vs. regexes to validate them
# Supporting 8 and 17 character identifiers
# AWS::EC2::AvailabilityZone::Name is handled separately by querying AWSPowerShell for AZs valid for the region we are executing in.
$Script:templateParameterValidators = @{

    'AWS::EC2::Image::Id'         = '^\s*ami-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Instance::Id'      = '^\s*i-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::SecurityGroup::Id' = '^\s*sg-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Subnet::Id'        = '^\s*subnet-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Volume::Id'        = '^\s*vol-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::VPC::Id'           = '^\s*vpc-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
}

# Check for and load AWSPowerShell
if (-not (Get-Module -ListAvailable | Where-Object {  $_.Name -ieq 'powershell-yaml' }))
{
    Write-Warning 'AWSPowerShell module not found on this system'
    Write-Warning 'Please install from the gallery'
    Write-Warning 'Install-Module -Name AWSPowerShell'
    throw 'AWSPowerShell not installed'
}

if (-not (Get-Module -Name AWSPowerShell -ErrorAction SilentlyContinue))
{
    Import-Module AWSPowerShell
}

# Check for YAML support
if (Get-Module -ListAvailable | Where-Object {  $_.Name -ieq 'powershell-yaml' })
{
    Import-Module powershell-yaml
    $script:yamlSupport = $true
}
else
{
    Write-Warning 'YAML support unavailable'
    Write-Warning 'To enable, install powershell-yaml from the gallery'
    Write-Warning 'Install-Module -Name powershell-yaml'
    $Script:yamlSupport = $false
}

#endregion

function New-Stack
{
    <#
    .SYNOPSIS
        Creates a stack.

    .DESCRIPTION
        Creates a stack.

        DYNAMIC PARAMETERS
            Once the -TemplateLocation argument has been suppied on the command line
            the function reads the template and creates additional command line parameters
            for each of the entries found in the "Parameters" section of the template.
            These parameters are named as per each parameter in the template and defaults
            and validation rules created for them as defined by the template.

            Thus, if a template parameter has AllowedPattern and AllowedValues properties,
            the resultant function argument will permit TAB completion of the AllowedValues,
            assert that you have entered one of these, and for AllowedPattern, the function
            argument will assert the regular expression.

            Template parameters with no default will become mandatory parameters to this function.
            If you do not supply them, you will be prompted for them and the help text for the 
            parameter will be taken from the Description property of the parameter.

    .PARAMETER StackName
        Name for the new stack.

    .PARAMETER TemplateLocation
        Location of the template. 
        This may be
        - Path to a local file
        - s3:// URL pointing to template in a bucket
        - https:// URL pointing to template in a bucket
        
    .PARAMETER Capabilities
        If the stack requires IAM capabilities, TAB auctocompletes between the capability types.
    
    .PARAMETER Wait
        If set, wait for stack creation to complete before returning.

    .OUTPUTS
        [string] ARN of the new stack
    #>    

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string]$StackName,
    
        [Parameter(Mandatory = $true)]
        [string]$TemplateLocation,

        [ValidateSet('CAPABILITY_IAM', 'CAPABILITY_NAMED_IAM', '')]
        [string]$Capabilities = '',
    
        [switch]$Wait
    )

    DynamicParam
    {
        New-TemplateDynamicParameters -TemplateLocation $TemplateLocation
    }

    begin
    {
        $stackParameters = Get-CommandLineStackParameters -BoundParameters $PSBoundParameters
    }

    end 
    {
        if (Test-StackExists -StackName $StackName)
        {
            throw "Stack already exists: $StackName"
        }

        $stackArgs = New-StackOperationArguments -StackName $StackName -TemplateLocation $TemplateLocation -Capabilities $Capabilities -StackParameters $stackParameters
        $arn = New-CFNStack @stackArgs

        if ($Wait)
        {
            Write-Host "Waiting for creation to complete"

            $stack = Wait-CFNStack -StackName $arn -Timeout ([TimeSpan]::FromMinutes(60).TotalSeconds) -Status @('CREATE_COMPLETE', 'ROLLBACK_IN_PROGRESS')

            if ($stack.StackStatus -like '*ROLLBACK*')
            {
                Write-Host -ForegroundColor Red "Create failed: $arn"
                Write-Host -ForegroundColor Red (Get-StackFailureEvents -StackName $arn | Sort-Object -Descending Timestamp | Out-String)

                throw $stack.StackStatusReason
            }
        }

        # Emit ARN
        $arn
    }
}

function Update-Stack
{
    <#
    .SYNOPSIS
        Updates a stack.

    .DESCRIPTION
        Updates a stack via creation and application of a changeset.


        DYNAMIC PARAMETERS
            Once the -TemplateLocation argument has been suppied on the command line
            the function reads the template and creates additional command line parameters
            for each of the entries found in the "Parameters" section of the template.
            These parameters are named as per each parameter in the template and defaults
            and validation rules created for them as defined by the template.

            Thus, if a template parameter has AllowedPattern and AllowedValues properties,
            the resultant function argument will permit TAB completion of the AllowedValues,
            assert that you have entered one of these, and for AllowedPattern, the function
            argument will assert the regular expression.

            Template parameters with no default will become mandatory parameters to this function.
            If you do not supply them, you will be prompted for them and the help text for the 
            parameter will be taken from the Description property of the parameter.

    .PARAMETER StackName
        Name for the new stack.

    .PARAMETER TemplateLocation
        Location of the template. 
        This may be
        - Path to a local file
        - s3:// URL pointing to template in a bucket
        - https:// URL pointing to template in a bucket
        
    .PARAMETER Capabilities
        If the stack requires IAM capabilities, TAB auctocompletes between the capability types.
    
    .PARAMETER Wait
        If set, wait for stack update to complete before returning.

    .PARAMETER Rebuild
        If set, update the stack by deleting then recreating it.

    .OUTPUTS
        [string] ARN of the new stack
    #>    
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [string]$StackName,

        [Parameter(Mandatory = $true)]
        [string]$TemplateLocation,

        [ValidateSet('CAPABILITY_IAM', 'CAPABILITY_NAMED_IAM', '')]
        [string]$Capabilities = '',
    
        [switch]$Wait,

        [switch]$Rebuild
    )

    DynamicParam
    {
        New-TemplateDynamicParameters -TemplateLocation $TemplateLocation
    }
    
    begin
    {
        $stackParameters = Get-CommandLineStackParameters -BoundParameters $PSBoundParameters
    }

    end 
    {
        if ($Rebuild)
        {
            # Pass all Update-Stack parameters except 'Rebuild' to New-Stack
            # Clone the bound parameters, excluding Rebuild argument
            $createParameters = @{}
            $PSBoundParameters.Keys |
            Where-Object { $_ -ine 'Rebuild'} |
            ForEach-Object {

                $createParameters.Add($_, $PSBoundParameters[$_])
            }

            Remove-Stack -StackName $StackName -Wait
            New-Stack @createParameters

            return
        }
        
        $changesetName = '{0}-{1}' -f [IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Module.Name), [int](([datetime]::UtcNow)-(get-date "1/1/1970")).TotalSeconds

        Write-Host "Creating change set $changesetName"

        $stackArgs = New-StackOperationArguments -StackName $StackName -TemplateLocation $TemplateLocation -Capabilities $Capabilities -StackParameters $stackParameters
        $csArn = New-CFNChangeSet -ChangeSetName $changesetName @stackArgs
        $cs = Get-CFNChangeSet -ChangeSetName $csArn

        while (('CREATE_COMPLETE', 'FAILED') -inotcontains $cs.Status)
        {
            Start-Sleep -Seconds 1
            $cs = Get-CFNChangeSet -ChangeSetName $csArn
        }

        if ($cs.Status -ieq 'FAILED')
        {
            throw "Changeset $changesetName failed to create"
        }

        Write-Host ($cs.Changes.ResourceChange | Select-Object Action, LogicalResourceId, PhysicalResourceId, ResourceType | Format-Table | Out-String)

        $choice = $host.ui.PromptForChoice(
            'Begin the stack update now?',
            $null,
            @(
                New-Object System.Management.Automation.Host.ChoiceDescription ('&Yes', "Start rebuild now." )
                New-Object System.Management.Automation.Host.ChoiceDescription ('&No', 'Abort operation.')
            ),
            0
        )
        
        if ($choice -ne 0)
        {
            throw "Aborted."
        }
        
        Write-Host "Updating stack $StackName"
        $arn = (Get-CFNStack -StackName $StackName).StackId
        Start-CFNChangeSet -StackName $StackName -ChangeSetName $changesetName

        if ($Wait)
        {
            Write-Host "Waiting for update to complete"

            $stack = Wait-CFNStack -StackName $arn -Timeout ([TimeSpan]::FromMinutes(60).TotalSeconds) -Status @('UPDATE_COMPLETE', 'UPDATE_ROLLBACK_IN_PROGRESS')

            if ($stack.StackStatus -like '*ROLLBACK*')
            {
                Write-Host -ForegroundColor Red "Update failed: $arn"
                Write-Host -ForegroundColor Red (Get-StackFailureEvents -StackName $arn | Sort-Object -Descending Timestamp | Out-String)

                throw $stack.StackStatusReason
            }

            # Emit ARN
            $arn
        }
        else
        {
            # Emit ARN
            $arn
        }
    }
}

function Remove-Stack
{
    <#
    .SYNOPSIS
        Delete one or more stacks.

    .DESCRIPTION
        Delete one or more stacks.

        Deletion of multiple stacks can be either sequential or parallel.
        If deleting a gruop of stacks where there are dependencies between them
        use the -Sequential switch and list the stacks in dependency order.

    .PARAMETER StackName
        Either stack names or the object returned by Get-CFNStack, New-CFNStack, Update-CFNStack 
        and other functions in this module when run with -Wait.
    
    .PARAMETER Wait
        If set and -Sequential is not set (so deleting in parallel), wait for all stacks to be deleted before returning.

    .PARAMETER Sequential
        If set, delete stacks in the order they are specified on the command line or received from the pipeline,
        waiting for each stack to delete successfully before proceeding to the next one.

    .EXAMPLE

        Remove-Stack -StackName MyStack

        Deletes a single stack.

    .EXAMPLE

        'DependentStack', 'BaseStack' | Remove-Stack -Sequential

        Deletes 'DependentStack', waits for completion, then deletes 'BaseStack'.
        
    .EXAMPLE

        'Stack1', 'Stack2' | Remove-Stack -Wait

        Sets both stacks deleting in parallel, then waits for them both to complete.
        
    .EXAMPLE

        'Stack1', 'Stack2' | Remove-Stack

        Sets both stacks deleting in parallel, and returns immediately. 
        See the CloudFormation console to monitor progress.
        
    .EXAMPLE

        Get-CFNStack | Remove-Stack

        You would NOT want to do this, just like you wouldn't do rm -rf / ! It is for illustration only.
        Sets ALL stacks in the region deleting simultaneously, which would probably trash some stacks
        and then others would fail due to dependent resources.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [string[]]$StackName,

        [switch]$Wait,

        [switch]$Sequential
    )

    begin
    {
        $endStates = @('DELETE_COMPLETE', 'DELETE_FAILED')
    }

    process
    {
        $arns = $StackName |
        ForEach-Object {

            if (Test-StackExists -StackName $_)
            {
                $arn = (Get-CFNStack -StackName $_).StackId

                Remove-CFNStack -StackName $arn -Force

                if ($Sequential -or ($Wait -and ($StackName | Measure-Object).Count -eq 1))
                {
                    # Wait for this delete to complete before starting the next
                    Write-Host "Waiting for delete: $arn"
                    $stack = Wait-CFNStack -StackName $arn -Timeout ([TimeSpan]::FromMinutes(60).TotalSeconds) -Status $endStates

                    if ($stack.StackStatus -like 'DELETE_FAILED')
                    {
                        Write-Host -ForegroundColor Red "Delete failed: $arn"
                        Write-Host -ForegroundColor Red (Get-StackFailureEvents -StackName $arn | Sort-Object -Descending Timestamp | Out-String)

                        # Have to give up now as chained stack almost certainly is used by this one
                        throw $stack.StackStatusReason
                    }
                }
                else 
                {
                    $arn
                }
            }
            else 
            {
                Write-Warning "Stack does not exist: $StackName"    
            }
        }
    }

    end
    {
        if ($Wait -and ($arns | Measure-Object).Count -gt 0)
        {
            Write-Host "Waiting for delete:`n$($arns -join "`n")"
            
            while ($arns.Length -gt 0)
            {
                Start-Sleep -Seconds 5

                foreach ($arn in $arns)
                {
                    $stack = Get-CFNStack -StackName $arn

                    if ($endStates -icontains $stack.StackStatus)
                    {
                        $arns = $arns | Where-Object { $_ -ine $arn }

                        if ($stack.StackStatus -like 'DELETE_FAILED')
                        {
                            Write-Host -ForegroundColor Red "Delete failed: $arn"
                            Write-Host -ForegroundColor Red "$($stack.StackStatusReason)"
                            Write-Host -ForegroundColor Red (Get-StackFailureEvents -StackName $StackName | Sort-Object -Descending Timestamp | Out-String)
                        }
                        else 
                        {
                            Write-Host "Delete complete: $arn"
                        }
                    }
                }
            }
        }
    }
}


# Helper Functions

function New-StackOperationArguments
{
    param
    (
        [string]$StackName,
        [string]$TemplateLocation,
        [string]$Capabilities,
        [object]$StackParameters
    )

    $stackArgs = @{

        'StackName' = $StackName
    }

    $template = New-TemplateResolver -TemplateLocation $TemplateLocation

    if ($template.IsFile)
    {
        $stackArgs.Add('TemplateBody', $template.ReadTemplate())
    }
    else 
    {
        $stackArgs.Add('TemplateURL', $template.Url)
    }

    if (-not [string]::IsNullOrEmpty($Capabilities))
    {
        $stackArgs.Add('Capabilities', @($Capabilities))
    }
        
    if (($stackParameters | Measure-Object).Count -gt 0)
    {
        $stackArgs.Add('Parameter', $stackParameters)
    }

    $stackArgs
}

function Test-StackExists
{
    <#
    .SYNOPSIS
        Tests whether a stack exists.

    .PARAMETER StackName
        Stack to test

    .OUTPUTS 
        [bool] true if stack exists; else false
    #>
    param
    (
        [string]$StackName
    )

    try 
    {
        Get-CFNStack -StackName $StackName
        $true    
    }
    catch 
    {
        $false    
    }
}

function Get-StackFailureEvents
{
    <#
    .SYNOPSIS
        Gets failure event list from a briken stack

    .DESCRIPTION 
        Gets failure events for a failed stack and also attempts
        to get the events for any nested stack. This depends on
        the functionbeing able to get to the nested stack resource
        before AWS removes it.

    .PARAMETER StackName 
        Name of failed stack

    .OUTPUTS
        [Amazon.CloudFormation.Model.StackEvent[]]
        Array of stack failure events.
    #>
    param
    (
        [string]$StackName
    )

    Get-CFNStackEvent -StackName $StackName |
    Where-Object {
        $_.ResourceStatus -ilike '*FAILED*' -or $_.ResourceStatus -ilike '*ROLLBACK*'
    }

    Get-CFNStackResourceList -StackName $StackName |
    Where-Object {
        $_.ResourceType -ieq 'AWS::CloudFormation::Stack'
    } |
    ForEach-Object {

        if ($_ -and $_.PhysicalResourceId)
        {
            Get-StackFailureEvents -StackName $_.PhysicalResourceId
        }
    }
}

function New-TemplateResolver
{
    <#
    .SYNOPSIS
        Resolve template location from path/url given on command lines

    .DESCRIPTION
        Returns an object that has methods for retrieving the template body
        and the size of the template file such that it can be checked for size limitations

    .PARAMETER TemplateLocation
        Location of the template. May be either
        - Path to local file
        - S3 URI (which is converted to HTTPS URI for the current region)
        - HTTP(S) Uri

    .OUTPUTS
        Custom Object.
    #>

    param 
    (
        [string]$TemplateLocation
    )

    $resolver = New-Object PSObject -Property @{

        'IsFile' = $null
        'BucketName' = $null
        'Key' = $null
        'Path' = $null
        'Url' = $null
    } |
    Add-Member -PassThru -Name ReadTemplate -MemberType ScriptMethod -Value {

        # Reads the template contents from either S3 or file system as approriate.
        if ($this.Path)
        {
            Get-Content -Raw -Path $this.Path
        }
        elseif ($this.BucketName -and $this.Key)
        {
            $tmpFile = "$([Guid]::NewGuid().ToString()).tmp"

            try {
                Read-S3Object -BucketName $this.BucketName -Key $this.Key -File $tmpFile | Out-Null
                Get-Content -Raw -Path $tmpFile                
            }
            finally {
                Remove-Item -Path $tmpFile
            }
        }
        else 
        {
            throw "Template location undefined"    
        }
    } |
    Add-Member -PassThru -Name Length -MemberType ScriptMethod -Value {

        # Gets the file szie of the template
        if ($this.Path)
        {
            (Get-ItemProperty -Name Length -Path $this.Path).Length
        }
        elseif ($this.BucketName -and $this.Key)
        {
            (Get-S3Object -BucketName $this.BucketName -Key $this.Key).Size
        }
        else 
        {
            throw "Template location undefined"    
        }
    }

    $u = $null

    if ([Uri]::TryCreate($TemplateLocation, 'Absolute', [ref]$u))
    {
        switch ($u.Scheme)
        {
            's3' {

                $r = Get-DefaultAWSRegion

                # Convert to full URL
                if (-not $r)
                {
                    throw "Cannot determine region. Please use Initialize-AWSDefaults or Set-DefaultAWSRegion"
                }

                $resolver.Url = [Uri]("https://s3-{0}.amazonaws.com/{1}{2}" -f $r.Region, $u.Authority, $u.LocalPath)
                $resolver.BucketName = $u.Authority
                $resolver.Key = $u.LocalPath
                $resolver.IsFile = $false
            }

            'file' {

                $resolver.Path = $TemplateLocation
                $resolver.IsFile = $true
            }

            { $_ -ieq 'http' -or $_ -ieq 'https' } {

                $resolver.Url = $u
                $resolver.BucketName = $u.Segments[1].Trim('/');
                $resolver.Key = $u.Segments[2..($u.Segments.Length-1)] -join ''
                $resolver.IsFile = $false
            }

            default {

                throw "Unsupported URI: $($u.ToString())"
            }
        }
    }
    else 
    {
        $resolver.Path = $TemplateLocation   
        $resolver.IsFile = $true 
    }
    
    $resolver
}


#region Dynamic Parameter magic for extracting template parameters
function Get-CommandLineStackParameters
{
    <#
    .SYNOPSIS
        Returns stack parameter objects from the calling function's command line

    .DESCRIPTION
        Discovers the parameters of the calling object that are dynamic and
        creates an array of stack parameter objects from them

    .PARAMETER BoundParameters
        The value of $PSBoundParameters of the calling function

    .OUTPUTS
        [Amazon.CloudFormation.Model.Parameter[]]
        Array of any parameters found.

    #>    
    param
    (
        [hashtable]$BoundParameters
    )

    $stackParameters = @()

    #This standard block of code loops through bound parameters...
    #If no corresponding variable exists, one is created
    #Get common parameters, pick out bound parameters not in that set
    function _temp { [cmdletbinding()] param() }

    $commonParameters = (Get-Command _temp | Select-Object -ExpandProperty parameters).Keys

    $BoundParameters.Keys | 
        Where-Object { 

        -not ($commonParameters -contains $_ -or (Get-Variable -Name $_ -Scope 1 -ErrorAction SilentlyContinue))
    } |
        ForEach-Object {

        # Now we are iterating the names of template parameters found on the command line.

        $stackParameters += $(

            $param = New-Object Amazon.CloudFormation.Model.Parameter 
            $param.ParameterKey = $_
            $param.ParameterValue = $BoundParameters.$_ -join ','
            $param            
        )
    }

    #Appropriate variables should now be defined and accessible
    $stackParameters
}

function New-TemplateDynamicParameters
{
    <#
    .SYNOPSIS
        Create PowerShell dynamic parameters from template parameters.

    .DESCRIPTION 
        Loads/downloads the template and parses the template body to extract arguments.
        Turns these parameters into PowerShell dynamic parameters for the
        New-PSCfnStack and Update-PSCfnStack CmdLets, also applying any
        constraints indicated by AllowedPattern or AllowedValues, and
        creating regex contraints to validate AWS types like AWS::EC2::Image::Id

    .PARAMETER TemplateLocation
        Location of the template. May be either
        - Path to local file
        - S3 URI (which is converted to HTTPS URI for the current region)
        - HTTP(S) Uri
        
    .OUTPUTS
        [System.Management.Automation.RuntimeDefinedParameterDictionary]
        New dynamic parameters to apply to caller.
    #>    
    param
    (
        [string]$TemplateLocation
    )
    
    #Create the RuntimeDefinedParameterDictionary
    $Dictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary

    (Get-TemplateParameters -TemplateResolver (New-TemplateResolver -TemplateLocation $TemplateLocation)).PSObject.Properties |
        ForEach-Object {

        $param = $_

        $paramDefinition = @{
            'Name'         = $param.Name
            'DPDictionary' = $Dictionary
        }

        if (-not $param.Value.PSObject.Properties['Type'])
        {
            throw "Malformed parameter definition. Type is required"
        }

        $awsType = $param.Value.Type

        # If it's one of the AWS special types that can equate to a regex
        if ($Script:templateParameterValidators.ContainsKey($awsType))
        {
            $paramDefinition.Add('Type', 'String')
            $paramDefinition.Add('ValidatePattern', $Script:templateParameterValidators[$awstype])
        }
        elseif ($awsType -imatch 'List\<(?<ResourceId>[A-Z0-9\:]+)\>' -and $Script:templateParameterValidators.ContainsKey($Matches.ResourceId))
        {
            $paramDefinition.Add('Type', 'String[]')
            $paramDefinition.Add('ValidatePattern', $Script:templateParameterValidators[$Matches.ResourceId])
        }
        elseif ($awsType -ieq 'AWS::EC2::AvailabilityZone::Name')
        {
            $paramDefinition.Add('Type', 'String')
            $paramDefinition.Add('ValidateSet', ((Get-EC2AvailabilityZone).ZoneName -join ','));
        }
        elseif ($awsType -ieq 'List<AWS::EC2::AvailabilityZone::Name>')
        {
            $paramDefinition.Add('Type', 'String[]')
            $paramDefinition.Add('ValidateSet', ((Get-EC2AvailabilityZone).ZoneName -join ','));
        }
        else
        {
            if ($awsType -ieq 'Number')
            {
                $paramDefinition.Add('Type', 'Double')
            }
            else 
            {
                $paramDefinition.Add('Type', 'String')
            }

            if ($param.Value.PSObject.Properties['AllowedValues'])
            {
                $paramDefinition.Add('ValidateSet', $param.Value.AllowedValues);
            }

            if ($param.Value.PSObject.Properties['AllowedPattern'])
            {
                $paramDefinition.Add('ValidatePattern', $param.Value.AllowedPattern);
            }
        }

        if ($param.Value.PSObject.Properties['Description'])
        {
            $paramDefinition.Add('HelpMessage', $param.Value.Description);
        }

        if (-not $param.Value.PSObject.Properties['Default'])
        {
            $paramDefinition.Add('Mandatory', $true);
        }

        New-DynamicParam @paramDefinition
    }
                  
    #return RuntimeDefinedParameterDictionary
    $Dictionary
}

function Get-TemplateParameters
{
    <#
        .SYNOPSIS
            Extract template parameter blok as a PowerShell object graph.

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

    # Check YAML/JSON
    try 
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
    catch
    {
        if (-not $Script:yamlSupport)
        {
            throw "Template cannot be parsed as JSON parse failed and YAML support unavailable"
        }
    }

    # Try YAML
    $templateObject = $template | ConvertFrom-Yaml 

    if ($templateObject.PSObject.Properties.Name -contains 'Parameters')
    {
        return $templateObject.Parameters
    }
}

function New-DynamicParam
{
    <#
        .SYNOPSIS
            Helper function to simplify creating dynamic parameters
        
        .DESCRIPTION
            Helper function to simplify creating dynamic parameters
    
            Example use cases:
                Include parameters only if your environment dictates it
                Include parameters depending on the value of a user-specified parameter
                Provide tab completion and intellisense for parameters, depending on the environment
    
            Please keep in mind that all dynamic parameters you create will not have corresponding variables created.
               One of the examples illustrates a generic method for populating appropriate variables from dynamic parameters
               Alternatively, manually reference $PSBoundParameters for the dynamic parameter value
    
        .NOTES
            Credit to http://jrich523.wordpress.com/2013/05/30/powershell-simple-way-to-add-dynamic-parameters-to-advanced-function/
                Added logic to make option set optional
                Added logic to add RuntimeDefinedParameter to existing DPDictionary
                Added a little comment based help
    
            Credit to BM for alias and type parameters and their handling

            Cresit to https://github.com/RamblingCookieMonster/PowerShell for this implementation
                Added ValidatePattern argument
    
        .PARAMETER Name
            Name of the dynamic parameter
    
        .PARAMETER Type
            Type for the dynamic parameter.  Default is string
    
        .PARAMETER Alias
            If specified, one or more aliases to assign to the dynamic parameter
    
        .PARAMETER ValidateSet
            If specified, set the ValidateSet attribute of this dynamic parameter
    
        .PARAMETER ValidatePattern
            If specified, set the ValidatePattern attribute of this dynamic parameter
    
        .PARAMETER Mandatory
            If specified, set the Mandatory attribute for this dynamic parameter
    
        .PARAMETER ParameterSetName
            If specified, set the ParameterSet attribute for this dynamic parameter
    
        .PARAMETER Position
            If specified, set the Position attribute for this dynamic parameter
    
        .PARAMETER ValueFromPipelineByPropertyName
            If specified, set the ValueFromPipelineByPropertyName attribute for this dynamic parameter
    
        .PARAMETER HelpMessage
            If specified, set the HelpMessage for this dynamic parameter
        
        .PARAMETER DPDictionary
            If specified, add resulting RuntimeDefinedParameter to an existing RuntimeDefinedParameterDictionary (appropriate for multiple dynamic parameters)
            If not specified, create and return a RuntimeDefinedParameterDictionary (appropriate for a single dynamic parameter)
    
            See final example for illustration
    
        .EXAMPLE
            
            function Show-Free
            {
                [CmdletBinding()]
                Param()
                DynamicParam {
                    $options = @( gwmi win32_volume | %{$_.driveletter} | sort )
                    New-DynamicParam -Name Drive -ValidateSet $options -Position 0 -Mandatory
                }
                begin{
                    #have to manually populate
                    $drive = $PSBoundParameters.drive
                }
                process{
                    $vol = gwmi win32_volume -Filter "driveletter='$drive'"
                    "{0:N2}% free on {1}" -f ($vol.Capacity / $vol.FreeSpace),$drive
                }
            } #Show-Free
    
            Show-Free -Drive <tab>
    
        # This example illustrates the use of New-DynamicParam to create a single dynamic parameter
        # The Drive parameter ValidateSet populates with all available volumes on the computer for handy tab completion / intellisense
    
        .EXAMPLE
    
        # I found many cases where I needed to add more than one dynamic parameter
        # The DPDictionary parameter lets you specify an existing dictionary
        # The block of code in the Begin block loops through bound parameters and defines variables if they don't exist
    
            Function Test-DynPar{
                [cmdletbinding()]
                param(
                    [string[]]$x = $Null
                )
                DynamicParam
                {
                    #Create the RuntimeDefinedParameterDictionary
                    $Dictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
            
                    New-DynamicParam -Name AlwaysParam -ValidateSet @( gwmi win32_volume | %{$_.driveletter} | sort ) -DPDictionary $Dictionary
    
                    #Add dynamic parameters to $dictionary
                    if($x -eq 1)
                    {
                        New-DynamicParam -Name X1Param1 -ValidateSet 1,2 -mandatory -DPDictionary $Dictionary
                        New-DynamicParam -Name X1Param2 -DPDictionary $Dictionary
                        New-DynamicParam -Name X3Param3 -DPDictionary $Dictionary -Type DateTime
                    }
                    else
                    {
                        New-DynamicParam -Name OtherParam1 -Mandatory -DPDictionary $Dictionary
                        New-DynamicParam -Name OtherParam2 -DPDictionary $Dictionary
                        New-DynamicParam -Name OtherParam3 -DPDictionary $Dictionary -Type DateTime
                    }
            
                    #return RuntimeDefinedParameterDictionary
                    $Dictionary
                }
                Begin
                {
                    #This standard block of code loops through bound parameters...
                    #If no corresponding variable exists, one is created
                        #Get common parameters, pick out bound parameters not in that set
                        Function _temp { [cmdletbinding()] param() }
                        $BoundKeys = $PSBoundParameters.keys | Where-Object { (get-command _temp | select -ExpandProperty parameters).Keys -notcontains $_}
                        foreach($param in $BoundKeys)
                        {
                            if (-not ( Get-Variable -name $param -scope 0 -ErrorAction SilentlyContinue ) )
                            {
                                New-Variable -Name $Param -Value $PSBoundParameters.$param
                                Write-Verbose "Adding variable for dynamic parameter '$param' with value '$($PSBoundParameters.$param)'"
                            }
                        }
    
                    #Appropriate variables should now be defined and accessible
                        Get-Variable -scope 0
                }
            }
    
        # This example illustrates the creation of many dynamic parameters using New-DynamicParam
            # You must create a RuntimeDefinedParameterDictionary object ($dictionary here)
            # To each New-DynamicParam call, add the -DPDictionary parameter pointing to this RuntimeDefinedParameterDictionary
            # At the end of the DynamicParam block, return the RuntimeDefinedParameterDictionary
            # Initialize all bound parameters using the provided block or similar code
    
        .FUNCTIONALITY
            PowerShell Language
    
    #>
    param(
        
        [string]
        $Name,
        
        [System.Type]
        $Type = [string],
    
        [string[]]
        $Alias = @(),
    
        [string[]]
        $ValidateSet,
        
        [string[]]
        $ValidatePattern,
        
        [switch]
        $Mandatory,
        
        [string]
        $ParameterSetName = "__AllParameterSets",
        
        [int]
        $Position,
        
        [switch]
        $ValueFromPipelineByPropertyName,
        
        [string]
        $HelpMessage,
    
        [validatescript( {
                if (-not ( $_ -is [System.Management.Automation.RuntimeDefinedParameterDictionary] -or -not $_) )
                {
                    Throw "DPDictionary must be a System.Management.Automation.RuntimeDefinedParameterDictionary object, or not exist"
                }
                $True
            })]
        $DPDictionary = $false
     
    )
    #Create attribute object, add attributes, add to collection   
    $ParamAttr = New-Object System.Management.Automation.ParameterAttribute
    $ParamAttr.ParameterSetName = $ParameterSetName
    if ($mandatory)
    {
        $ParamAttr.Mandatory = $True
    }
    if ($Position -ne $null)
    {
        $ParamAttr.Position = $Position
    }
    if ($ValueFromPipelineByPropertyName)
    {
        $ParamAttr.ValueFromPipelineByPropertyName = $True
    }
    if ($HelpMessage)
    {
        $ParamAttr.HelpMessage = $HelpMessage
    }
     
    $AttributeCollection = New-Object 'Collections.ObjectModel.Collection[System.Attribute]'
    $AttributeCollection.Add($ParamAttr)
        
    #param validation set if specified
    if ($ValidateSet)
    {
        $ParamOptions = New-Object System.Management.Automation.ValidateSetAttribute -ArgumentList $ValidateSet
        $AttributeCollection.Add($ParamOptions)
    }
    
    #param validation pattern if specified
    if ($ValidatePattern)
    {
        $ParamOptions = New-Object System.Management.Automation.ValidatePatternAttribute -ArgumentList $ValidatePattern
        $AttributeCollection.Add($ParamOptions)
    }
    
    #Aliases if specified
    if ($Alias.count -gt 0)
    {
        $ParamAlias = New-Object System.Management.Automation.AliasAttribute -ArgumentList $Alias
        $AttributeCollection.Add($ParamAlias)
    }
    
     
    #Create the dynamic parameter
    $Parameter = New-Object -TypeName System.Management.Automation.RuntimeDefinedParameter -ArgumentList @($Name, $Type, $AttributeCollection)

    #Add the dynamic parameter to an existing dynamic parameter dictionary, or create the dictionary and add it
    if ($DPDictionary)
    {
        $DPDictionary.Add($Name, $Parameter)
    }
    else
    {
        $Dictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $Dictionary.Add($Name, $Parameter)
        $Dictionary
    }
}

#endregion

Export-ModuleMember -Function *
