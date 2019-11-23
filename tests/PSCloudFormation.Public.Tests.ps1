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

# Import MockS3
. (Join-Path $PSScriptRoot MockS3.class.ps1)

# https://github.com/PowerShell/PowerShell/issues/2408#issuecomment-251140889
InModuleScope $ModuleName {

    Describe 'New-PSCFNStack' {

        $regionList = (Get-AWSRegion).Region

        Mock -CommandName Get-EC2Region -MockWith {

            $regionList |
            ForEach-Object {

                New-Object PSObject -Property @{ RegionName = $_ }
            }
        }

        Mock -CommandName Get-CFNStack -MockWith {

            throw New-Object System.InvalidOperationException ('Stack does not exist')
        }

        Mock -CommandName Wait-CFNStack -MockWith {

            return @{
                StackStatus = 'CREATE_COMPLETE'
            }
        }

        foreach ($ext in @('json', 'yaml'))
        {
            Context 'Parameter errors' {

                It "Should throw with invalid CIDR ($($ext.ToUpper()))" {

                    { New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 999.0.0.0/16 } | Should Throw
                }

                It "Should throw with a value that is not in AllowedValues ($($ext.ToUpper()))" {

                    { New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.0.0.0/16 -DnsSupport BreakMe } | Should Throw
                }

                It "Should throw with invalid region parameter ($($ext.ToUpper()))" {

                    { New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -VpcCidr 10.0.0.0/16 -Region eu-west-9 } | Should Throw
                }
            }

            $regionList |
            Foreach-Object {
                $region = $_

                Context "Successful stack creation - $region" {

                    $expectedArn = "arn:aws:cloudformation:$region:000000000000:stack/pester/00000000-0000-0000-0000-000000000000"

                    Mock -CommandName New-CFNStack -MockWith {

                        return $expectedArn
                    }

                    $stackArn = New-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -VpcCidr 10.0.0.0/16 -Region $region

                    It 'Should have called Get-CFNStack' {

                        Assert-MockCalled -CommandName Get-CFNStack -Times 1 -Scope Context
                    }

                    It 'Should have called New-CFNStack' {

                        Assert-MockCalled -CommandName Get-CFNStack -Times 1 -Scope Context
                    }

                    It 'Should return stack ARN' {

                        $stackArn | Should -Be $expectedArn
                    }
                }
            }
        }
    }

    Describe 'Update-PSCFNStack' {

        $regionList = (Get-AWSRegion).Region

        Mock -CommandName Get-EC2Region -MockWith {

            $regionList |
            ForEach-Object {

                New-Object PSObject -Property @{ RegionName = $_ }
            }
        }

        Mock -CommandName Get-CFNStack -MockWith {


            if ($StackName -like '*pester*')
            {
                return @{
                    StackParameters = @(
                        New-Object Amazon.CloudFormation.Model.Parameter -Property @{
                            ParameterKey = 'VpcCidr'
                            ParameterValue = '10.0.0.0/16'
                            UsePreviousValue = $true
                        }
                    )
                    StackId         = $global:TestStackArn
                }
            }
            elseif ($StackName -like '*unchanged*')
            {
                return @{
                    StackParameters = @(
                        New-Object Amazon.CloudFormation.Model.Parameter -Property @{
                            ParameterKey = 'VpcCidr'
                            ParameterValue = '10.0.0.0/16'
                            UsePreviousValue = $true
                        }
                    )
                    StackId         = $global:UnchangedStackArn
                }
            }
            else
            {
                throw New-Object System.InvalidOperationException ('Stack does not exist')
            }
        }

        Mock -CommandName Get-CFNStackResourceList -MockWith {}

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

        Mock -CommandName Wait-PSCFNStack -MockWith {

            $true
        }

        Mock -CommandName Get-CFNTemplate -MockWith {

            Get-Content -Raw "$($global:TestStackFilePathWithoutExtension).json"
        }

        $mocks = @('Get-CFNStack', 'Get-CFNStackResourceList', 'New-CFNChangeSet', 'Get-CFNChangeSet', 'Start-CFNChangeSet', 'Wait-CFNStack', 'Get-CFNTemplate' )

        (Get-AWSCmdletName -Service CFN).CmdletName |
        ForEach-Object {
            $_.Replace('CFNCFN', 'CFN')
        } |
        Where-Object {
            $mocks -inotcontains $_
        } |
        Foreach-Object {
            $cmdlet = $_
            try {
                Mock -CommandName $cmdlet -MockWith { Write-Warning "$cmdlet not implemented" }
            }
            catch {
                Write-Warning "Unable to mock $_"
            }
        }

        foreach ($ext in @('json', 'yaml'))
        {
            It "Should update when stack exists ($($ext.ToUpper()))" {

                Update-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.1.0.0/16 -Force
            }

            It "Should fail when stack does not exist ($($ext.ToUpper()))" {

                { Update-PSCFNStack -StackName DoesNotExist -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.0.0.0/16 } | Should Throw
            }

            It "Should throw with invalid region parameter ($($ext.ToUpper()))" {

                { Update-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.1.0.0/16 -Region eu-west-9 } | Should Throw
            }

            $regionList |
            ForEach-Object {

                $region = $_

                It "Should not throw with valid region $region ($($ext.ToUpper()))" {

                    { Update-PSCFNStack -StackName pester -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.1.0.0/16 -Region $region -Force } | Should Not Throw
                }
            }

            It "Should not throw if no changes are detected ($($ext.ToUpper()))" {

                Update-PSCFNStack -StackName unchanged -TemplateLocation "$($global:TestStackFilePathWithoutExtension).$($ext)" -Wait -VpcCidr 10.0.0.0/16 -Force | Should -Be $global:UnchangedStackArn
            }
        }

        It "Should update stack with -UsePreviousTemplate" {

            Update-PSCFNStack -StackName pester -UsePreviousTemplate -Wait -VpcCidr 10.1.0.0/16 -Force
        }
    }

    Describe 'Remove-PSCFNStack' {

        $regionList = (Get-AWSRegion).Region

        Mock -CommandName Get-EC2Region -MockWith {

            $regionList |
            ForEach-Object {

                New-Object PSObject -Property @{ RegionName = $_ }
            }
        }

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


        Mock -CommandName Wait-PSCFNStack -MockWith {

            $true
        }

        $mocks = @('Get-CFNStack', 'Wait-CFNStack', 'Remove-CFNStack' )

        (Get-AWSCmdletName -Service CFN).CmdletName |
        ForEach-Object {
            $_.Replace('CFNCFN', 'CFN')
        } |
        Where-Object {
            $mocks -inotcontains $_
        } |
        Foreach-Object {
            $cmdlet = $_
            try {
                Mock -CommandName $cmdlet -MockWith { Write-Warning "$cmdlet not implemented" }
            }
            catch {
                Write-Warning "Unable to mock $_"
            }
        }

        It 'Should return nothing if stack does not exist' {

            Remove-PSCFNStack -StackName DoesNotExist -Force | Should BeNullOrEmpty
        }

        It 'Should return ARN if stack exists' {

            Remove-PSCFNStack -StackName pester -Force | Should Be $global:TestStackArn
        }

        It 'Should throw with invalid region parameter' {

            { Remove-PSCFNStack -StackName pester -Force -Region eu-west-9 } | Should Throw
        }

        Context 'Should delete existing stack in valid region' {

            $regionList |
            ForEach-Object {

                $region = $_
                $expectedArn = "arn:aws:cloudformation:$($region):000000000000:stack/pester/00000000-0000-0000-0000-000000000000"

                Mock -CommandName Get-CFNStack -ParameterFilter { $StackName -eq 'pester' } -MockWith {

                    return @{
                        StackId = "arn:aws:cloudformation:$($region):000000000000:stack/pester/00000000-0000-0000-0000-000000000000"
                    }
                }

                It "In $region" {

                    Remove-PSCFNStack -StackName pester -Force -Region $region | Should -Be $expectedArn
                }
            }
        }
    }

    Describe 'Get-PSCFNStackOutputs' {

        $vpcId =  @{
            Description = "ID of the new VPC"
            ExportName  = "test-stack-VpcId"
            OutputKey   = "VpcId"
            OutputValue = "vpc-00000000"
        }

        $subnetId =  @{
            Description = "ID of the new Subnet"
            OutputKey   = "SubnetId"
            OutputValue = "subnet-00000000"
        }

        Mock -CommandName Get-CFNStack -MockWith {

            return @{
                StackId   = $global:TestStackArn
                StackName = 'test-stack'
                Outputs   = @(
                    $vpcId
                    $subnetId
                )
            }
        }

        Context 'Gets stack outputs as a parameter block - 2 outputs' {

            $parameters = Get-PSCFNStackOutputs -StackName test-stack -AsParameterBlock

            It 'Should return 2 parameters' {

                $parameters.PSObject.Properties | Measure-Object | Select-Object -ExpandProperty Count | Should -Be 2
            }

            ('VpcId', 'SubnetId') |
            Foreach-Object {

                $parameterName = $_
                $parameterValue = Invoke-Command ([scriptblock]::Create("`$$parameterName"))

                It "Should have declared $parameterName Parameter" {

                    $parameters.PSObject.Properties.Name | Should -Contain $parameterName
                }

                It "Sets parameter description from output description (parameter $parameterName)" {

                    $parameters.$parameterName.Description | Should -Be $parameterValue.Description
                }

                It "Sets parameter default from output's value (parameter $parameterName)" {

                    $parameters.$parameterName.Default | Should Be $parameterValue.OutputValue
                }
            }

            It 'Should have parsed parameter type correctly (VpcId parameter value is AWS::EC2::VPC::Id)' {

                $parameters.VpcId.Type | Should Be 'AWS::EC2::VPC::Id'
            }

            It 'Should have parsed parameter type correctly (SubnetId parameter value is AWS::EC2::Subnet::Id)' {

                $parameters.SubnetId.Type | Should Be 'AWS::EC2::Subnet::Id'
            }
        }

        Context 'Gets stack outputs as a mapping block' {

            $result = Get-PSCFNStackOutputs -StackName test-stack -AsMappingBlock

            It 'Should get correct value for VpcId' {

                $result.VpcId | Should Be "vpc-00000000"
            }

            It 'Should get correct value for SubnetId' {

                $result.SubnetId | Should Be "subnet-00000000"
            }
        }

        Context 'Gets stack outputs as a cross-stack reference (2 parameters, only one exported' {

            $result = Get-PSCFNStackOutputs -StackName test-stack -AsCrossStackReferences

            It 'Should provide one export' {

                $result.PSObject.Properties | Measure-Object | Select-Object -ExpandProperty Count | Should -Be 1
            }

            It 'Should export VpcId' {

                $result.VpcId.'Fn::ImportValue'.'Fn::Sub' | Should Be '${TestStackStack}-VpcId'
            }
        }

    }
}