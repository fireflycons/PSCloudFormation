# Argument completer for -Select argument.
# Do not modify this file; it may be overwritten during version upgrades.

$psMajorVersion = $PSVersionTable.PSVersion.Major
if ($psMajorVersion -eq 2)
{
    Write-Verbose "Dynamic argument completion not supported in PowerShell version 2; skipping load."
    return
}

# PowerShell's native Register-ArgumentCompleter cmdlet is available on v5.0 or higher. For lower
# version, we can use the version in the TabExpansion++ module if installed.
$registrationCmdletAvailable = ($psMajorVersion -ge 5) -Or !($null -eq (Get-Command Register-ArgumentCompleter -ea Ignore))

# internal function to perform the registration using either cmdlet or manipulation
# of the options table
function _pscfnArgumentCompleterRegistration()
{
    param
    (
        [scriptblock]$scriptBlock,
        [hashtable]$param2CmdletsMap
    )

    if ($registrationCmdletAvailable)
    {
        foreach ($paramName in $param2CmdletsMap.Keys)
        {
            $arguments = @{
                "ScriptBlock" = $scriptBlock
                "Parameter"   = $paramName
            }

            $cmdletNames = $param2CmdletsMap[$paramName]
            if ($cmdletNames -And $cmdletNames.Length -gt 0)
            {
                $arguments["Command"] = $cmdletNames
            }

            Register-ArgumentCompleter @arguments
        }
    }
    else
    {
        if (-not $global:options) { $global:options = @{ CustomArgumentCompleters = @{ }; NativeArgumentCompleters = @{ } } }

        foreach ($paramName in $param2CmdletsMap.Keys)
        {
            $cmdletNames = $param2CmdletsMap[$paramName]

            if ($cmdletNames -And $cmdletNames.Length -gt 0)
            {
                foreach ($cn in $cmdletNames)
                {
                    $fqn = [string]::Concat($cn, ":", $paramName)
                    $global:options['CustomArgumentCompleters'][$fqn] = $scriptBlock
                }
            }
            else
            {
                $global:options['CustomArgumentCompleters'][$paramName] = $scriptBlock
            }
        }

        $function:tabexpansion2 = $function:tabexpansion2 -replace 'End\r\n{', 'End { if ($null -ne $options) { $options += $global:options} else {$options = $global:options}'
    }
}


$PSCFN_SelectCompleters = {
    param
    (
        $commandName,
        $parameterName,
        $wordToComplete,
        $commandAst,
        $fakeBoundParameter
    )

    $binaryCommandName = $commandName.Replace('-PSCFN', [string]::Empty) + 'Command'
    $cmdletType = Invoke-Expression "[Firefly.PSCloudFormation.Commands.$($binaryCommandName)]"

    if (-not $cmdletType)
    {
        return
    }

    $v = @( '*' )

    $outputProperties = $cmdletType.GetProperties(('Instance', 'NonPublic')) |
    Where-Object {
        $_.GetCustomAttributes([Firefly.PSCloudFormation.SelectableOutputPropertyAttribute], $true)
    } |
    Select-Object -ExpandProperty Name |
    Sort-Object

    if ($outputProperties)
    {
        $v += $outputProperties
    }

    $parameters = $cmdletType.GetProperties(('Instance', 'Public')) |
    Where-Object {
        $_.GetCustomAttributes([System.Management.Automation.ParameterAttribute], $true) -and 
        -not $_.GetCustomAttributes([Firefly.PSCloudFormation.SuppressParameterSelectAttribute], $true) -and
        $_.PropertyType -ne [System.Management.Automation.SwitchParameter]
    } |
    Select-Object -ExpandProperty Name |
    Sort-Object

    if ($parameters)
    {
        $v += (
            $parameters |
            ForEach-Object {
                "^$_"
            }
        )
    }

    $v |
    Where-Object { $_ -match "^$([System.Text.RegularExpressions.Regex]::Escape($wordToComplete)).*" } |
    ForEach-Object { New-Object System.Management.Automation.CompletionResult $_, $_, 'ParameterValue', $_ }
}

$PSCFN_SelectMap = @{
    Select = @(
        'New-PSCFNStack'
        'Update-PSCFNStack'
        'Reset-PSCFNStack'
        'Remove-PSCFNStack'
        'New-PSCFNChangeset'
    )
}

_pscfnArgumentCompleterRegistration $PSCFN_SelectCompleters $PSCFN_SelectMap

