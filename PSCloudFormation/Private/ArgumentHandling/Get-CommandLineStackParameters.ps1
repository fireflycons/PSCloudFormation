function Get-CommandLineStackParameters
{
    <#
    .SYNOPSIS
        Returns stack parameter objects from the calling function's command line

    .DESCRIPTION
        Discovers the parameters of the calling object that are dynamic and
        creates an array of stack parameter objects from them

    .PARAMETER CallerBoundParameters
        Value of $PSBoundParameters from the calling function

    .OUTPUTS
        [Amazon.CloudFormation.Model.Parameter[]]
        Array of any parameters found.

    #>
    param
    (
        [hashtable]$CallerBoundParameters
    )

    # Load any file-based parameters first
    $fileParameters = @()

    if ($CallerBoundParameters.ContainsKey("ParameterFile") -and $null -ne $CallerBoundParameters["ParameterFile"])
    {
        $fileParameters = (Get-Content -Path $CallerBoundParameters["ParameterFile"] -Raw | ConvertFrom-Json) |
        Foreach-Object {
            # Convert param structure back to hashtable
            $h = @{}
            $_.PSObject.Properties |
            ForEach-Object {
                $h.Add($_.Name, $_.Value)
            }

            # Emit parameter object
            New-Object Amazon.CloudFormation.Model.Parameter -Property $h
        }
    }

    # Now get command line dynamic parameters
    # Create a dummy function for the purpose of discovering
    # the PowerShell common parameters so they can be filtered out.
    function _temp { [cmdletbinding()] param() }

    $commonParameters = (Get-Command _temp | Select-Object -ExpandProperty parameters).Keys

    $stackParameters = $CallerBoundParameters.Keys |
        Where-Object {

        # Filter out common parameters, AWS common credential parameters and any explicit command line parameters (defined as variables at scope 1)
        -not ($commonParameters -contains $_ -or $Script:CommonCredentialArguments.Keys -contains $_ -or (Get-Variable -Name $_ -Scope 1 -ErrorAction SilentlyContinue))
    } |
        ForEach-Object {

        # Now we are iterating the names of dynamic template parameters found on the command line.

        $param = New-Object Amazon.CloudFormation.Model.Parameter
        $param.ParameterKey = $_
        $param.ParameterValue = $CallerBoundParameters.$_ -join ','
        $param
    }

    # Merge file parameters with command line parameters. Command line takes precedence
    if (($fileParameters | Measure-Object).Count -gt 0)
    {
        if (($stackParameters | Measure-Object).Count -eq 0)
        {
            $stackParameters = $fileParameters
        }
        else
        {
            # Ensure stackParameters is an array
            $stackParameters = ,$stackParameters

            foreach($fp in $fileParameters)
            {
                if (-not ($stackParameters | Where-Object { $_.ParameterKey -eq $fp.ParameterKey}))
                {
                    $stackParameters += $fp
                }
            }
        }
    }

    # We want this to return an array - always
    switch (($stackParameters | Measure-Object).Count)
    {
        0
        {
            # Stupid, stupid
            # https://stackoverflow.com/questions/18476634/powershell-doesnt-return-an-empty-array-as-an-array
            $a = @()
            return , $a
        }

        1
        {
            # Stupid, stupid
            # https://stackoverflow.com/questions/18476634/powershell-doesnt-return-an-empty-array-as-an-array
            $a = @($stackParameters)
            return , $a
        }

        default
        {
            return $stackParameters
        }
    }

}
