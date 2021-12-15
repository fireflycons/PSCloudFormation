function Get-PropertyNameFromSlug
{
    <#
        .SYNOPSIS
            Gets the MSBuild property name from tag slug e.g. cfn/1.0.0
    #>
    param
    (
        [Parameter(Mandatory)]
        [string] $tagSlug
    )

    switch ($tagSlug)
    {
        'core' { "Generate_PSCloudFormation" }
        default { throw "Invalid tag slug." }
    }
}

function Update-PackagesGeneration
{
    <#
        .SYNOPSIS
            Update the PackagesGeneration.props based on given tag name.
    #>
    param
    (
        [Parameter(Mandatory)]
        [string] $propertyName
    )

    # Update the package generation props to enable package generation of the right package
    $genPackagesFilePath = "./build/PackagesGeneration.props"
    $genPackagesContent = Get-Content $genPackagesFilePath
    $newGenPackagesContent = $genPackagesContent -replace "<$propertyName>\w+<\/$propertyName>", "<$propertyName>true</$propertyName>"
    $newGenPackagesContent | Set-Content $genPackagesFilePath

    # Check content changes (at least one property changed
    $genPackagesContentStr = $genPackagesContent | Out-String
    $newGenPackagesContentStr = $newGenPackagesContent | Out-String
    if ($genPackagesContentStr -eq $newGenPackagesContentStr)
    {
        throw "MSBuild property $propertyName does not exist in $genPackagesFilePath or content not updated."
    }
}

function Update-AllPackagesGeneration
{
    <#
        .SYNOPSIS
            Update the PackagesGeneration.props to generate all packages.
    #>
    # Update the package generation props to enable package generation of the right package
    $genPackagesFilePath = "./build/PackagesGeneration.props"
    $genPackagesContent = Get-Content $genPackagesFilePath
    $newGenPackagesContent = $genPackagesContent -replace "false", "true"
    $newGenPackagesContent | Set-Content $genPackagesFilePath
}

function Update-DeployBuild
{
    <#
        .SYNOPSIS
            Update the DeployDuild.props to make the build a deploy build.
    #>
    # Update the package generation props to enable package generation of the right package
    $genPackagesFilePath = "./build/DeployBuild.props"
    $genPackagesContent = Get-Content $genPackagesFilePath
    $newGenPackagesContent = $genPackagesContent -replace "false", "true"
    $newGenPackagesContent | Set-Content $genPackagesFilePath
}

########################################################################

# Update .props based on git tag status & setup build version
if ($env:APPVEYOR_REPO_TAG -eq "true")
{
    Update-DeployBuild
    $tagParts = $env:APPVEYOR_REPO_TAG_NAME.split("/", 2)

    # Full release
    if ($tagParts.Length -eq 1) # X.Y.Z(.R)
    {
        if (-not ($env:APPVEYOR_REPO_TAG_NAME -match '(?<ver>\d+\.\d+\.\d+(\.\d+)?)'))
        {
            throw "Invalid tag version: $env:APPVEYOR_REPO_TAG_NAME"
        }

        $version = $Matches.ver

        Update-AllPackagesGeneration
        $env:PSCFN_BuildVersion = $version
        $env:PSCFN_ReleaseName = $env:APPVEYOR_REPO_TAG_NAME
    }
    # Partial release
    else # Slug/X.Y.Z
    {
        # Retrieve MSBuild property name for which enabling package generation
        $tagSlug = $tagParts[0]
        $propertyName = Get-PropertyNameFromSlug $tagSlug
        $tagVersion = $tagParts[1]

        Update-PackagesGeneration $propertyName
        $env:PSCFN_BuildVersion = $tagVersion
        $projectName = $propertyName -replace "Generate_", ""
        $projectName = $projectName -replace "_", "."
        $env:PSCFN_ReleaseName = "$projectName $tagVersion"
    }

    $env:IsFullIntegrationBuild = $false # Run only tests on deploy builds (not coverage, etc.)
}
else
{
    Update-AllPackagesGeneration
    $env:PSCFN_BuildVersion = "$($env:APPVEYOR_BUILD_VERSION)"
    $env:PSCFN_ReleaseName = $env:PSCFN_BuildVersion

    $env:IsFullIntegrationBuild = "$env:APPVEYOR_PULL_REQUEST_NUMBER" -eq "" -And $env:Configuration -eq "Release"
}

$env:Build_Assembly_Version = "$env:PSCFN_BuildVersion" -replace "\-.*", ""

"Building version: $env:PSCFN_BuildVersion"
"Building assembly version: $env:Build_Assembly_Version"

