function Test-IsFileSystemPath
{
<#
    .SYNOPSIS
        Test if the input is a filesystem path.

    .DESCRIPTION
        Used by the cloudformation packager to determine if a property value
        is a filesystem path and the target should be packaged to S3.

    .PARAMETER PropertyValue
        Value of cloudformation property to test.

    .OUTPUTS
        [boolean]
        True if the value represents a path.
#>
    param
    (
        [object]$PropertyValue
    )

    if (-not ($PropertyValue -is [string]))
    {
        # Not a string, then not a path
        return $false
    }

    try
    {
        if (Test-Path -Path $PropertyValue -PathType Any)
        {
            # Is a path that exists
            return $true
        }
    }
    catch
    {
        # Illegal path chars - can't be a path.
        return $false
    }

    try
    {
        $uri = [Uri]$PropertyValue

        if ($uri.Scheme -ieq 'file')
        {
            # Definitely is a file
            return $true
        }

        if (-not ([string]::IsNullOrEmpty($uri.Scheme)))
        {
            # It is an internet URI of some description (S3, HTTPS etc.)
            return $false
        }
    }
    catch
    {
        # If it cannot be parsed as a URI, definitely not a path. Most likely an inline lambda function.
        return $false
    }

    # If we get here, we have to assume it's a file.
    # All properties checked except AWS::Lambda::Function ... Code are URI properties.
    # Lambda code is an object entry which should have failed at one of the other checks above
    $true
}