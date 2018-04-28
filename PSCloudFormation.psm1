
#region Module Init

$ErrorActionPreference = 'Stop'

$Script:yamlSupport = $false

$Script:templateParameterValidators = @{

    'AWS::EC2::Image::Id'         = '^\s*ami-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Instance::Id'      = '^\s*i-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::SecurityGroup::Id' = '^\s*sg-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Subnet::Id'        = '^\s*subnet-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::Volume::Id'        = '^\s*vol-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
    'AWS::EC2::VPC::Id'           = '^\s*vpc-([a-z0-9]{8}|[a-z0-9]{17})\s*$'
}

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
}

#endregion

function New-Stack
{
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
    Begin
    {
        $stackParameters = Get-CommandLineTemplateParameters -BoundParameters $PSBoundParameters
        $stackParameters
    }
}

function Update-Stack
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'ByName', ValueFromPipeline = $true)]
        [string]$StackName,
    
        [Parameter(Mandatory = $true, ParameterSetName = 'ByObject', ValueFromPipeline = $true)]
        [Amazon.CloudFormation.Model.Stack]$Stack,
        
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
    Begin
    {
        $stackParameters = Get-CommandLineTemplateParameters -BoundParameters $PSBoundParameters
        $stackParameters
    }
}

function Remove-Stack
{
    [Parameter(Mandatory = $true, ParameterSetName = 'ByName', ValueFromPipeline = $true)]
    [string]$StackName,

    [Parameter(Mandatory = $true, ParameterSetName = 'ByObject', ValueFromPipeline = $true)]
    [Amazon.CloudFormation.Model.Stack]$Stack,

    [switch]$Wait
}

#region Dynamic Parameter magic for extracting template parameters

function Get-CommandLineTemplateParameters
{
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
    param
    (
        [string]$TemplateLocation
    )
    
    #Create the RuntimeDefinedParameterDictionary
    $Dictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary

    (Get-TemplateParameters -TemplateLocation $TemplateLocation).PSObject.Properties |
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
    param
    (
        [string]$TemplateLocation
    )

    $u = $null
    $template = $null

    if ([Uri]::TryCreate($TemplateLocation, 'Absolute', [ref]$u) -and $u.Scheme -ne 'file')
    {
        # Template is a URL
        switch ($u.Scheme)
        {
            {$_ -ieq 'http' -or $_ -ieq 'https'}
            {

                $template = Invoke-WebRequest -Uri $u -UseBasicParsing -Method Get
            }

            's3'
            {

                $tmpFile = "$([Guid]::NewGuid().Guid.ToString()).tmp"

                try 
                {
                    Get-S3Object -BucketName $u.Authority -Key $u.LocalPath -File $tmpFile
                    # Assert file length
                    $template = Get-Content -Raw $tmpFile
                }
                finally 
                {
                    Remove-Item $tmpFile
                }
            }
        }
    }
    else
    {
        # Assert file length
        $template = Get-Content -Raw -Path $TemplateLocation
    }

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
            throw "Template cannot be parsed as JSON and YAML support unavailable"
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

            Original version of this function with notes above from https://github.com/RamblingCookieMonster/PowerShell
            
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
