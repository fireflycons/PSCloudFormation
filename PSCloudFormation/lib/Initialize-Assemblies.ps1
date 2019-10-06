$here = Split-Path -Parent $MyInvocation.MyCommand.Path

function Import-YamlAssembly
{
    $assemblies = @{
        "core"  = Join-Path $here "netstandard1.3\YamlDotNet.dll";
        "net45" = Join-Path $here "net45\YamlDotNet.dll";
        "net35" = Join-Path $here "net35\YamlDotNet.dll";
    }

    if ($PSVersionTable.PSEdition -eq "Core")
    {
        return [Reflection.Assembly]::LoadFrom($assemblies["core"])
    }
    elseif ($PSVersionTable.PSVersion.Major -ge 4)
    {
        return [Reflection.Assembly]::LoadFrom($assemblies["net45"])
    }
    else
    {
        return [Reflection.Assembly]::LoadFrom($assemblies["net35"])
    }
}


function Initialize-Assemblies
{
    # .NET compression libraries
    'System.IO.Compression', 'System.IO.Compression.FileSystem' |
        Foreach-Object {
            [System.Reflection.Assembly]::LoadWithPartialName($_)
        }

    # YamlDotNet
    $requiredTypes = @(
        "YamlStream"
        "YamlMappingNode"
        "YamlSequenceNode"
        "YamlScalarNode"
    )

    $yaml = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object Location -Match "YamlDotNet.dll"

    if (!$yaml)
    {
        return Import-YamlAssembly
    }

    # TODO - Handle multiple versions of the assembly being present
    <#
    if ($yaml.ManifestModule.Assembly.GetName().Version -lt [Version]"6.0.0.0")
    {
        throw "Version $($yaml.ManifestModule.Assembly.GetName().Version) has been loaded by something else. At least version 6.0.0.0 is required. Reset your session and load this module first."
    }
    #>
}

Initialize-Assemblies | Out-Null