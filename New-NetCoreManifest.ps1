<#
    .SYNOPSIS
        Generates a new Powershell Core module manifest from the Windows PowerShell one

    .DESCRIPTION
        Reads in PSCloudFormation.psd1 and writes out PSCloudFormation.netcore.psd1 making the following alterations
        Adds
            - CompatiblePSEditions = @('Core')
        Modifies
            GUID = a different GUID
            RequiredModules = @('AWSPowerShell.netcore')
            PowerShellVersion = "6.0"
            PrivateData\PSData\ExternalModuleDependencies = @('AWSPowerShell.netcore')
#>
function RenderArray
{
<#
    .SYNOPSIS
        Render an array of strings to PowerShell code, i.e. @( ... )

    .PARAMETER Arry
        Array to render

    .OUTPUTS
        Array definition
#>
    param
    (
        [Parameter(Position = 0)]
        [Array]$Arry
    )

    $sb = New-Object System.Text.StringBuilder

    $sb.Append("@(").
    Append((($arry | ForEach-Object { "`"$_`"" }) -join ', ')).
    Append(")").
    ToString()
}

function RenderHashTable
{
<#
    .SYNOPSIS
        Recursively render a hashtable to PowerShell code, i.e. @{ ... }
        Not smart - only expects the types found in a manifest file.

    .PARAMETER Arry
        Hash to render

    .OUTPUTS
        Hash definition
#>
param
    (
        [hashtable]$Hash,
        [int]$Level = 0
    )

    $sb = New-Object System.Text.StringBuilder
    $sb.AppendLine("@{") | Out-Null

    $Hash.Keys |
    ForEach-Object {
        $value = $Hash[$_]
        $pad = " " * ($Level + 1) * 4

        if ($value -is [Array])
        {
            $sb.AppendLine(("{0}{1} = {2}" -f $pad, $_, (RenderArray $value))) | Out-Null
        }
        elseif ($value -is [hashtable])
        {
            $sb.AppendLine(("{0}{1} = {2}" -f $pad, $_, (RenderHashTable -Hash $value -Level ($Level + 1)))) | Out-Null
        }
        else
        {
            $sb.AppendLine(("{0}{1} = `"{2}`"" -f $pad, $_, $value)) | Out-Null
        }
    }

    $sb.AppendLine(((" " * $Level * 4) + "}")) | Out-Null
    $sb.ToString()
}

# Input manifest
$manifestFile = [IO.Path]::Combine($PSScriptRoot, "PSCloudFormation", "PSCloudFormation.psd1")

# Output manifest
$netCoreManifestFile = [IO.Path]::Combine($PSScriptRoot, "PSCloudFormation", "PSCloudFormation.netcore.psd1")

# GUID for netcore module
$netcoreGuid = '87c7f071-2c52-4fb7-9348-17de474650b8'

# Read manifest to hashtable
$manifest = Invoke-Expression "$(Get-Content -Raw $manifestFile)"

# Update values
$manifest['CompatiblePSEditions'] = @('Core')
$manifest['GUID'] = $netcoreGuid
$manifest['RequiredModules'] = @('AWSPowerShell.netcore')
$manifest['PowerShellVersion'] = '6.0'
$manifest['PrivateData']['PSData']['ExternalModuleDependencies'] = @('AWSPowerShell.netcore')

# Render hash back to powershell code
$netCoreManifest = RenderHashtable -Hash $manifest

# UTF8 without BOM encoding
$enc = New-Object System.Text.UTF8Encoding -ArgumentList $false

# Write new manifest
[IO.File]::WriteAllText($netCoreManifestFile, $netCoreManifest, $enc)

Write-Host "Generated $netCoreManifestFile"