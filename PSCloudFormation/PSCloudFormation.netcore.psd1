@{
    Copyright = "(c) 2018 Alistair Mackay. All rights reserved."
    HelpInfoURI = "https://github.com/fireflycons/PSCloudFormation"
    CompatiblePSEditions = @("Core")
    PrivateData = @{
        PSData = @{
            ProjectUri = "https://github.com/fireflycons/PSCloudFormation"
            ReleaseNotes = "https://github.com/fireflycons/PSCloudFormation/blob/master/RELEASENOTES.md"
            LicenseUri = "https://github.com/fireflycons/PSCloudFormation/blob/master/LICENSE"
            Tags = @("AWS", "CloudFormation")
            ExternalModuleDependencies = @("AWSPowerShell.netcore")
        }

    }

    GUID = "87c7f071-2c52-4fb7-9348-17de474650b8"
    Author = "Alistair Mackay"
    VariablesToExport = @()
    FunctionsToExport = @("Get-PSCFNStackOutputs", "New-PSCFNStack", "Remove-PSCFNStack", "Reset-PSCFNStack", "Update-PSCFNStack", "New-PSCFNPackage")
    CompanyName = "Firefly Consulting Ltd."
    RootModule = "PSCloudFormation.psm1"
    PowerShellVersion = "6.0"
    Description = "Wrapper for CloudFormation Deployments"
    CmdletsToExport = @()
    ModuleVersion = "1.8.0"
    AliasesToExport = @()
    RequiredModules = @("AWSPowerShell.netcore")
}
