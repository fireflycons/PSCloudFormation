function Update-EndpointValue
{
<#
    .SYNOPSIS
        Rewrite EndpointURL argument to appropriate localstack port

    .PARAMETER CredentialArguments
        Argument hash to examine

    .PARAMETER Service
        Service whose endpoint URL we want to set.

    .OUTPUTS
        New hash of arguments if EndpointURL was present; else input arguments
#>
    param
    (
        [hashtable]$CredentialArguments = @{},

        [ValidateSet("S3", "CF")]
        [string]$Service
    )

    if ($CredentialArguments.Keys -inotcontains 'EndpointURL' )
    {
        return $CredentialArguments
    }

    # To support localstack testing, we have to fudge EndpointURL if present
    $outputArguments = @{}

    $CredentialArguments.Keys |
    ForEach-Object {

        $value = $CredentialArguments[$_]

        if ($_ -ieq 'EndpointUrl')
        {
            $ub = [UriBuilder]$value

            if (('localstack', 'localhost') -icontains $ub.Host)
            {
                # For these hosts, assume we are using localstack, else leave unchanged
                $ub.Port = $script:localStackPorts.$Service
                $value = $ub.ToString()
            }
        }

        $outputArguments.Add($_, $value)
    }

    $outputArguments
}