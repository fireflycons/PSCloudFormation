function RenderArray
{
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

$manifestFile = [IO.Path]::Combine($PSScriptRoot, "PSCloudFormation", "PSCloudFormation.psd1")
$netCoreManifestFile = [IO.Path]::Combine($PSScriptRoot, "PSCloudFormation", "PSCloudFormation.netcore.psd1")
$netcoreGuid = '87c7f071-2c52-4fb7-9348-17de474650b8'

$manifest = Invoke-Expression "$(Get-Content -Raw $manifestFile)"

$manifest['CompatiblePSEditions'] = @('Core')
$manifest['GUID'] = $netcoreGuid
$manifest['RequiredModules'] = @('AWSPowerShell.netcore')
$manifest['PowerShellVersion'] = '6.0'
$manifest['PrivateData']['PSData']['ExternalModuleDependencies'] = @('AWSPowerShell.netcore')

$netCoreManifest = RenderHashtable -Hash $manifest

$enc = New-Object System.Text.UTF8Encoding -ArgumentList $false

[IO.File]::WriteAllText($netCoreManifestFile, $netCoreManifest, $enc)

Write-Host "Generated $netCoreManifestFile"