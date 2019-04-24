$ModuleName = $(
    if ($PSVersionTable.PSEdition -ieq 'Core')
    {
        'PSCloudFormation.netcore'
    }
    else
    {
        'PSCloudFormation'
    }
)

$global:haveYaml = $null -ne (Get-Module -ListAvailable | Where-Object { $_.Name -ieq 'powershell-yaml' })

# http://www.lazywinadmin.com/2016/05/using-pester-to-test-your-manifest-file.html
# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = Get-ChildItem -Path (Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition)) -Recurse -Filter "$ModuleName.psd1" | Select-Object -ExpandProperty FullName

if (($ManifestFile | Measure-Object).Count -ne 1)
{
    throw "Cannot locate $ModuleName.psd1"
}

$global:TestStackArn = 'arn:aws:cloudformation:us-east-1:000000000000:stack/pester/00000000-0000-0000-0000-000000000000'
$global:UnchangedStackArn = 'arn:aws:cloudformation:us-east-1:000000000000:stack/unchanged/00000000-0000-0000-0000-000000000000'

$global:TestStackFilePathWithoutExtension = Join-Path $PSScriptRoot test-stack

# Import the module and store the information about the module
$ModuleInformation = Import-Module -Name $ManifestFile -PassThru

Describe "$ModuleName Module - Testing Manifest File (.psd1)" {
    Context 'Manifest' {
        It 'Should contain RootModule' {
            $ModuleInformation.RootModule | Should not BeNullOrEmpty
        }
        It 'Should contain Author' {
            $ModuleInformation.Author | Should not BeNullOrEmpty
        }
        It 'Should contain Company Name' {
            $ModuleInformation.CompanyName | Should not BeNullOrEmpty
        }
        It 'Should contain Description' {
            $ModuleInformation.Description | Should not BeNullOrEmpty
        }
        It 'Should contain Copyright' {
            $ModuleInformation.Copyright | Should not BeNullOrEmpty
        }
        It 'Should contain License' {
            $ModuleInformation.LicenseURI | Should not BeNullOrEmpty
        }
        It 'Should contain a Project Link' {
            $ModuleInformation.ProjectURI | Should not BeNullOrEmpty
        }
        It 'Should contain Tags (For the PSGallery)' {
            $ModuleInformation.Tags.count | Should not BeNullOrEmpty
        }
    }
}

# https://github.com/PowerShell/PowerShell/issues/2408#issuecomment-251140889
InModuleScope $ModuleName {
    Describe 'PSCloudFormation - Public Interface' {

        Mock -CommandName Get-EC2Region -MockWith {

            @(
                New-Object PSObject -Property @{ RegionName = "ap-south-1" }
                New-Object PSObject -Property @{ RegionName = "eu-west-3" }
                New-Object PSObject -Property @{ RegionName = "eu-west-2" }
                New-Object PSObject -Property @{ RegionName = "eu-west-1" }
                New-Object PSObject -Property @{ RegionName = "ap-northeast-2" }
                New-Object PSObject -Property @{ RegionName = "ap-northeast-1" }
                New-Object PSObject -Property @{ RegionName = "sa-east-1" }
                New-Object PSObject -Property @{ RegionName = "ca-central-1" }
                New-Object PSObject -Property @{ RegionName = "ap-southeast-1" }
                New-Object PSObject -Property @{ RegionName = "ap-southeast-2" }
                New-Object PSObject -Property @{ RegionName = "eu-central-1" }
                New-Object PSObject -Property @{ RegionName = "us-east-1" }
                New-Object PSObject -Property @{ RegionName = "us-east-2" }
                New-Object PSObject -Property @{ RegionName = "us-west-1" }
                New-Object PSObject -Property @{ RegionName = "us-west-2" }
            )
        }

        Mock -CommandName Wait-PSCFNStack -MockWith {
            $true
        }

        Context 'New-PSCFNStack' {

            Mock -CommandName Get-CFNStack -MockWith {

                throw New-Object System.InvalidOperationException ('Stack does not exist')
            }

            Mock -CommandName New-CFNStack -MockWith {

                return $global:TestStackArn
            }

            Mock -CommandName Wait-CFNStack -MockWith {

                return @{
                    StackStatus = 'CREATE_COMPLETE'
                }
            }

            foreach ($ext in @('json', 'yaml'))
            {
                It "Should create stack and return ARN with valid command line arguments ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.0.0.0/16 | Should Be $TestStackArn
                }

                It "Should throw with invalid CIDR ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    { New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 999.0.0.0/16 } | Should Throw
                }

                It "Should throw with a value that is not in AllowedValues ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    { New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.0.0.0/16 -DnsSupport BreakMe } | Should Throw
                }

                It "Should throw with invalid region parameter ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    { New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -VpcCidr 10.0.0.0/16 -Region eu-west-9 } | Should Throw
                }

                It "Should not throw with valid region parameter ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    @(
                        'ap-northeast-1'
                        'ap-northeast-2'
                        'ap-south-1'
                        'ap-southeast-1'
                        'ap-southeast-2'
                        'ca-central-1'
                        'eu-central-1'
                        'eu-west-1'
                        'eu-west-2'
                        'eu-west-3'
                        'sa-east-1'
                        'us-east-1'
                        'us-east-2'
                        'us-west-1'
                        'us-west-2'
                    ) |
                    ForEach-Object {

                        Write-Host -ForegroundColor DarkGreen "      [?] - Testing region $($_)"
                        { New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -VpcCidr 10.0.0.0/16 -Region $_ } | Should Not Throw
                    }
                }
            }
        }

        Context 'Update-PSCFNStack' {

            Mock -CommandName Get-CFNStack -ParameterFilter { $StackName -eq 'DoesNotExist' } -MockWith {

                throw New-Object System.InvalidOperationException ('Stack does not exist')
            }

            Mock -CommandName Get-CFNStack -ParameterFilter { $StackName -eq 'pester' } -MockWith {

                return @{
                    StackParameters = @(
                        New-Object Amazon.CloudFormation.Model.Parameter |
                        ForEach-Object {
                            $_.ParameterKey = 'VpcCidr'
                            $_.ParameterValue = '10.0.0.0/16'
                            $_.UsePreviousValue = $true
                            $_
                        }
                    )
                    StackId         = $global:TestStackArn
                }
            }

            Mock -CommandName Get-CFNStack -ParameterFilter { $StackName -eq 'unchanged' } -MockWith {

                return @{
                    StackParameters = @(
                        New-Object Amazon.CloudFormation.Model.Parameter |
                        ForEach-Object {
                            $_.ParameterKey = 'VpcCidr'
                            $_.ParameterValue = '10.0.0.0/16'
                            $_.UsePreviousValue = $true
                            $_
                        }
                    )
                    StackId         = $global:UnchangedStackArn
                }
            }

            Mock -CommandName New-CFNChangeSet -MockWith {

                if (-not $TemplateURL -and -not $TemplateBody -and -not $UsePreviousTemplate)
                {
                    throw 'New-CFNChangeSet : Either Template URL or Template Body must be specified.'
                }

                return 'arn:aws:cloudformation:us-east-1:000000000000:changeSet/SampleChangeSet/1a2345b6-0000-00a0-a123-00abc0abc000'
            }

            Mock -CommandName New-CFNChangeSet -ParameterFilter { $StackName -eq 'unchanged' } -MockWith {

                return 'arn:aws:cloudformation:us-east-1:000000000000:changeSet/Unchanged/1a2345b6-0000-00a0-a123-00abc0abc000'
            }

            Mock -CommandName Get-CFNChangeSet -MockWith {

                return @{
                    Status  = 'CREATE_COMPLETE'
                    Changes = @{
                        ResourceChange = @(
                            New-Object PSObject -Property @{
                                Action             = 'Modify'
                                LogicalResourceId  = 'Vpc'
                                PhysicalResourceId = 'vpc-00000000'
                                ResourceType       = 'AWS::EC2::VPC'
                            }
                        )
                    }
                }
            }

            Mock -CommandName Get-CFNChangeSet -ParameterFilter { $ChangeSetName -eq 'arn:aws:cloudformation:us-east-1:000000000000:changeSet/Unchanged/1a2345b6-0000-00a0-a123-00abc0abc000' }  -MockWith {

                return @{
                    Status       = 'FAILED'
                    StatusReason = "The submitted information didn't contain changes. Submit different information to create a change set."
                }
            }

            Mock -CommandName Start-CFNChangeSet -MockWith { }

            Mock -CommandName Wait-CFNStack -MockWith {

                return @{
                    StackStatus = 'UPDATE_COMPLETE'
                }
            }

            Mock -CommandName Get-CFNTemplate -MockWith {

                Get-Content -Raw "$($global:TestStackFilePathWithoutExtension).json"
            }

            foreach ($ext in @('json', 'yaml'))
            {
                It "Should fail when stack does not exist ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    { Update-PSCFNStack -StackName DoesNotExist -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.0.0.0/16 } | Should Throw
                }

                It "Should update when stack exists ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    Update-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.1.0.0/16 -Force
                }

                It "Should throw with invalid region parameter ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    { Update-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.1.0.0/16 -Region eu-west-9 } | Should Throw
                }

                It "Should not throw with valid region parameter ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    @(
                        'ap-northeast-1'
                        'ap-northeast-2'
                        'ap-south-1'
                        'ap-southeast-1'
                        'ap-southeast-2'
                        'ca-central-1'
                        'eu-central-1'
                        'eu-west-1'
                        'eu-west-2'
                        'eu-west-3'
                        'sa-east-1'
                        'us-east-1'
                        'us-east-2'
                        'us-west-1'
                        'us-west-2'
                    ) |
                    ForEach-Object {
                        Write-Host -ForegroundColor DarkGreen "      [?] - Testing region $($_)"

                        { Update-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.1.0.0/16 -Region $_ -Force } | Should Not Throw
                    }
                }

                It "Should not throw if no changes are detected ($($ext.ToUpper()))" {

                    if ($ext -eq 'yaml' -and -not $global:haveYaml)
                    {
                        Set-ItResult -Inconclusive -Because "No YAML parser loaded"
                        return
                    }

                    Update-PSCFNStack -StackName unchanged -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.0.0.0/16 -Force | Should -Be $global:UnchangedStackArn
                }
            }

            It "Should update stack with -UsePreviousTemplate" {

                Update-PSCFNStack -StackName pester -UsePreviousTemplate -Wait -VpcCidr 10.1.0.0/16 -Force
            }
        }

        Context 'Remove-PSCFNStack' {

            Mock -CommandName Get-CFNStack -ParameterFilter { $StackName -eq 'DoesNotExist' } -MockWith {

                throw New-Object System.InvalidOperationException ('Stack does not exist')
            }

            Mock -CommandName Get-CFNStack -ParameterFilter { $StackName -eq 'pester' } -MockWith {

                return @{
                    StackId = $global:TestStackArn
                }
            }

            Mock -CommandName Remove-CFNStack { }

            Mock -CommandName Wait-CFNStack -MockWith {

                return @{
                    StackStatus = 'DELETE_COMPLETE'
                }
            }

            It 'Should return nothing if stack does not exist' {

                Remove-PSCFNStack -StackName DoesNotExist | Should BeNullOrEmpty
            }

            It 'Should return ARN if stack exists' {

                Remove-PSCFNStack -StackName pester | Should Be $global:TestStackArn
            }

            It 'Should throw with invalid region parameter' {

                { Remove-PSCFNStack -StackName pester -Region eu-west-9 } | Should Throw
            }

            It 'Should not throw with valid region parameter' {

                @(
                    'ap-northeast-1'
                    'ap-northeast-2'
                    'ap-south-1'
                    'ap-southeast-1'
                    'ap-southeast-2'
                    'ca-central-1'
                    'eu-central-1'
                    'eu-west-1'
                    'eu-west-2'
                    'eu-west-3'
                    'sa-east-1'
                    'us-east-1'
                    'us-east-2'
                    'us-west-1'
                    'us-west-2'
                ) |
                ForEach-Object {

                    Write-Host -ForegroundColor DarkGreen "      [?] - Testing region $($_)"
                    { Remove-PSCFNStack -StackName pester -Region $_ } | Should Not Throw
                }
            }
        }

        Context 'Get-PSCFNStackOutputs' {

            Mock -CommandName Get-CFNStack -MockWith {

                return @{
                    StackId   = $global:TestStackArn
                    StackName = 'test-stack'
                    Outputs   = @(
                        @{
                            Description = "ID of the new VPC"
                            ExportName  = "test-stack-VpcId"
                            OutputKey   = "VpcId"
                            OutputValue = "vpc-00000000"
                        }
                    )
                }
            }

            It 'Gets stack outputs as a parameter block' {

                $result = Get-PSCFNStackOutputs -StackName test-stack -AsParameterBlock

                $result.VpcId.Description | Should Be "ID of the new VPC"
                $result.VpcId.Default | Should Be "vpc-00000000"
                $result.VpcId.Type | Should Be 'AWS::EC2::VPC::Id'
            }

            It 'Gets stack outputs as a mapping block' {

                $result = Get-PSCFNStackOutputs -StackName test-stack -AsMappingBlock

                $result.VpcId | Should Be "vpc-00000000"
            }

            It 'Gets stack outputs as a cross-stack reference' {

                $result = Get-PSCFNStackOutputs -StackName test-stack -AsCrossStackReferences

                $result.VpcId.'Fn::ImportValue'.'Fn::Sub' | Should Be '${TestStackStack}-VpcId'
            }
        }
    }
}