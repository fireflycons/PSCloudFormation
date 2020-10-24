param
(
    [Version]$RequiredVersion,

    [switch]$AllVersions
)

$ErrorActionPreference = 'Stop'
$dependentsRx = [System.Text.RegularExpressions.Regex]::new('''(?<firstModule>[\w\d\.\-]+)(,(?<additionalModule>[\w\d\.\-]+)){0,}'' are dependent on this module', [System.Text.RegularExpressions.RegexOptions]::Singleline)


function Uninstall-ModuleWithDependencies
{
    param
    (
        [string]$Name,
        [hashtable]$Arguments,
        [System.Collections.Generic.List[object]] $UninstalledDependencies
    )

    if ($Arguments.ContainsKey('RequiredVersion'))
    {
        Write-Host "Uninstalling $Name $($Arguments['RequiredVersion'])"
    }
    else
    {
        Write-Host "Uninstalling $Name AllVersions"
    }

    $output = Uninstall-Module $Name @Arguments 2>&1 | Out-String
    $m = $dependentsRx.Match($output.Replace([Environment]::Newline, [string]::Empty))

    if ($m.Success)
    {
        Invoke-Command -NoNewScope {

            $m.Groups['firstModule'].Value
            $m.Groups['additionalModule'].Captures |
            ForEach-Object {
                $_.Value
            }
        } |
        ForEach-Object {

            Uninstall-ModuleWithDependencies -Name $_ -Arguments @{ AllVersions = $true } -UninstalledDependencies $UninstalledDependencies
            Get-Module -ListAvailable $_ |
            ForEach-Object {

                $UninstalledDependencies.Add($_)
            }
        }

        # Retry
        Uninstall-Module $Name @Arguments
    }
}

$versionParams = @{}

if ($PSBoundParameters.ContainsKey('RequiredVersion'))
{
    $versionParams.Add('RequiredVersion', $RequiredVersion)
}

if ($PSBoundParameters.ContainsKey('AllVersions'))
{
    $versionParams.Add('AllVersions', $true)
}

$aws = Get-Module -ListAvailable | Where-Object { $_.Name -like 'AWS.Tools.*' }

$common = $aws | Where-Object { $_.Name -eq 'AWS.Tools.Common' }
$installer = $aws | Where-Object { $_.Name -eq 'AWS.Tools.Installer' }
$modules = $aws | Where-Object { ('AWS.Tools.Installer', 'AWS.Tools.Common') -notcontains $_.Name }

$removedDependencies = [System.Collections.Generic.List[object]]::new()

$modules |
Group-Object Name |
Foreach-Object {

    $group = $_.Group
    $name = $_.Name

    $thisVersionParams = $versionParams.Clone()

    if ($versionParams.Count -eq 0)
    {
        $version = $group | Sort-Object -Descending Version | Select-Object -First 1 | Select-Object -ExpandProperty Version
        $thisVersionParams['RequiredVersion'] = $version
    }
    elseif ($versionParams.ContainsKey('RequiredVersion'))
    {
        $version = $versionParams['RequiredVersion']
    }

    Uninstall-ModuleWithDependencies -Name $name -Arguments $thisVersionParams -UninstalledDependencies $removedDependencies
}

# AWS.Tools.Common is last as all others depend on it.
$common |
Group-Object Name |
Foreach-Object {

    $group = $_.Group
    $name = $_.Name

    $thisVersionParams = $versionParams.Clone()

    if ($versionParams.Count -eq 0)
    {
        $version = $group | Sort-Object -Descending Version | Select-Object -First 1 | Select-Object -ExpandProperty Version
        $thisVersionParams['RequiredVersion'] = $version
    }
    elseif ($versionParams.ContainsKey('RequiredVersion'))
    {
        $version = $versionParams['RequiredVersion']
    }

    Uninstall-ModuleWithDependencies -Name $name -Arguments $thisVersionParams
}

if ($installer)
{
    Write-Host "Installers remaining:"
    Write-Host ($installer | Out-String)
}

if ($removedDependencies.Count -gt 0)
{
    Write-Host "Removed dependents:"
    Write-Host ($removedDependencies | Out-String)
}
