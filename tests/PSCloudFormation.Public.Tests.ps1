$ModuleName = 'PSCloudFormation'

# http://www.lazywinadmin.com/2016/05/using-pester-to-test-your-manifest-file.html
# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$global:ManifestFile = "$(Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition))\$ModuleName\$ModuleName.psd1"

$global:TestStackArn = 'arn:aws:cloudformation:us-east-1:000000000000:stack/pester/00000000-0000-0000-0000-000000000000'

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
InModuleScope 'PSCloudFormation' {
    Describe 'PSCloudFormation - Public Interface' {

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

            It 'Detects when a parameter required by the template (-VpcCidr) not given on command line' {

                # Normally, the UI will prompt for the requirte parameter,
                # and the user can get the parameter's decription via the usual mandatory parameter help mechanism.
                # However to test that the dynamic parameter is generated and required, we need to set it up to throw rather than prompt.
                # We can then test the exeption type and properties to validate the correct argument is required.
                # https://github.com/PowerShell/PowerShell/issues/2408#issuecomment-251140889
                # http://nivot.org/blog/post/2010/05/03/PowerShell20DeveloperEssentials1InitializingARunspaceWithAModule
                $ex = Invoke-Command -NoNewScope {
                    try
                    {
                        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault()
                        $iss.ImportPSModule($global:ManifestFile)
                        $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace($iss)
                        $rs.Open()
                        $ri = New-Object System.Management.Automation.RunSpaceInvoke($rs)
                        $ri.Invoke("New-PSCFNStack -StackName teststack -TemplateLocation '$PSScriptRoot\test-stack.json'")
                    }
                    catch
                    {
                        $_.Exception.InnerException
                    }
                    finally
                    {
                        ($ri, $rs) |
                            ForEach-Object {
                            if ($_)
                            {
                                $_.Dispose()
                            }
                        }
                    }
                }

                $ex | SHould BeOfType System.Management.Automation.ParameterBindingException
                $ex.ParameterName.Trim() | SHould Be 'VpcCidr'
            }

            It 'Should create stack and return ARN with valid command line arguments' {

                New-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -Wait -VpcCidr 10.0.0.0/16 | Should Be $TestStackArn
            }

            It 'Should throw with invalid CIDR' {

                { New-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -Wait -VpcCidr 999.0.0.0/16 } | Should Throw
            }

            It 'Should throw with a value that is not in AllowedValues' {

                { New-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -Wait -VpcCidr 10.0.0.0/16 -DnsSupport BreakMe } | Should Throw
            }

            It 'Should throw with invalid region parameter' {

                { New-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -VpcCidr 10.0.0.0/16 -Region eu-west-9 } | Should Throw
            }

            It 'Should not throw with valid region parameter' {

                Get-EC2Region |
                    ForEach-Object {

                    Write-Host -ForegroundColor DarkGreen "      [?] Testing region $($_.RegionName)"
                    { New-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -VpcCidr 10.0.0.0/16 -Region $_.RegionName } | Should Not Throw
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

            Mock -CommandName New-CFNChangeSet -MockWith {

                return 'arn:aws:cloudformation:us-east-1:000000000000:changeSet/SampleChangeSet/1a2345b6-0000-00a0-a123-00abc0abc000'
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

            Mock -CommandName Start-CFNChangeSet -MockWith {}

            Mock -CommandName Wait-CFNStack -MockWith {

                return @{
                    StackStatus = 'UPDATE_COMPLETE'
                }
            }

            It 'Should fail when stack does not exist' {

                { Update-PSCFNStack -StackName DoesNotExist -TemplateLocation "$PSScriptRoot\test-stack.json" -Wait -VpcCidr 10.0.0.0/16 } | Should Throw
            }

            It 'Should update when stack exists' {

                Update-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -Wait -VpcCidr 10.1.0.0/16 -Force
            }

            It 'Should throw with invalid region parameter' {

                { Update-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -Wait -VpcCidr 10.1.0.0/16 -Region eu-west-9 } | Should Throw
            }

            It 'Should not throw with valid region parameter' {

                Get-EC2Region |
                    ForEach-Object {
                    Write-Host -ForegroundColor DarkGreen "      [?] Testing region $($_.RegionName)"

                    { Update-PSCFNStack -StackName pester -TemplateLocation "$PSScriptRoot\test-stack.json" -Wait -VpcCidr 10.1.0.0/16 -Region $_.RegionName -Force } | Should Not Throw
                }
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

            Mock -CommandName Remove-CFNStack {}

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

                Get-EC2Region |
                    ForEach-Object {

                    Write-Host -ForegroundColor DarkGreen "      [?] Testing region $($_.RegionName)"
                    { Remove-PSCFNStack -StackName pester -Region $_.RegionName } | Should Not Throw
                }
            }
        }

        Context 'Get-PSCFNStackOutputs' {

            Mock -CommandName Get-CFNStack -MockWith {

                return @{
                    StackId = $global:TestStackArn
                    Outputs = @(
                        @{
                            Description = "ID of the new VPC"
                            ExportName  = "test-stack-VpcId"
                            OutputKey   = "VpcId"
                            OutputValue = "vpc-a2e2bfc4"
                        }
                    )
                }
            }

            It 'Gets stack outputs as a parameter block' {

                $result = Get-PSCFNStackOutputs -StackName test-stack -AsParameterBlock

                $result
            }
        }
    }
}