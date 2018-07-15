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

    # Create a dummy function for the purpose of dicovering
    # the PowerShell common parameters so they can be filtered out.
    function _temp { [cmdletbinding()] param() }

    $commonParameters = (Get-Command _temp | Select-Object -ExpandProperty parameters).Keys

    $stackParameters = $CallerBoundParameters.Keys |
        Where-Object {

        -not ($commonParameters -contains $_ -or $Script:commonCredentialArguments.Keys -contains $_ -or (Get-Variable -Name $_ -Scope 1 -ErrorAction SilentlyContinue))
    } |
        ForEach-Object {

        # Now we are iterating the names of template parameters found on the command line.

        $param = New-Object Amazon.CloudFormation.Model.Parameter
        $param.ParameterKey = $_
        $param.ParameterValue = $CallerBoundParameters.$_ -join ','
        $param
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
