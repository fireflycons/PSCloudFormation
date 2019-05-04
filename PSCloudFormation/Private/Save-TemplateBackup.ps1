function Save-TemplateBackup
{
    <#
    .SYNOPSIS
        Saves stack template with current parameters as backup files

    .PARAMETER StackName
        Stack name or ARN

    .PARAMETER CredentialArguments
        Credential arguments passed to public function.

#>
    param
    (
        [string]$StackName,

        [hashtable]$CredentialArguments = @{},

        [string]$OutputPath
    )

    $stackArguments = Update-EndpointValue -CredentialArguments $CredentialArguments -Service CF

    try
    {
        # Get the stack
        $stack = Get-CFNStack -StackName $StackName @stackArguments

        # Check for nested stacks - might support in future
        if (Get-CFNStackResourceList -StackName $stack.StackId @stackArguments | Where-Object { $_.ResourceType -eq 'AWS::CloudFormation::Stack' })
        {
            Write-Warning "Template backup doesn't currently support nested stacks."
        }
    }
    catch
    {
        # Stack not found
        return
    }

    # Get the template
    $template = Get-CFNTemplate -StackName $stack.StackId @stackArguments
    $encoding = New-Object System.Text.UTF8Encoding($false, $false)

    # Determine format
    $ext = $(

        try
        {
            $template | ConvertFrom-Json | Out-Null
            "json"
        }
        catch
        {
            "yaml"
        }
    )

    if (-not (Test-Path -PathType Container -Path $OutputPath))
    {
        New-Item -Path $OutputPath -ItemType Directory | Out-Null
    }

    $OutputPath = (Resolve-Path $OutputPath).Path

    # Write template
    $templatePath = Join-Path $OutputPath "$($stack.StackName).template.bak.$($ext)"
    [IO.File]::WriteAllText($templatePath, $template, $encoding)

    if ($stack.Parameters.Count -gt 0)
    {
        # Write out parameter file
        $parameterPath = Join-Path $OutputPath "$($stack.StackName).parameters.bak.json"
        [IO.File]::WriteAllText($parameterPath, ($stack.Parameters | Select-Object PArameterKey, ParameterValue | ConvertTo-Json), $encoding)
        Write-Host "Template backed up. Revert changes with the following arguments"
        Write-Host "-StackName $StackName -TemplateLocation '$templatePath' -ParameterFile '$parameterPath'"
    }
    else
    {
        Write-Host "Template backed up. Revert changes with the following arguments"
        Write-Host "-StackName $StackName -TemplateLocation '$templatePath'"
    }
}