function Get-Executable
{
    param
    (
        [string]$name
    )

    $command = $null
    foreach ($exe in @($name, "$name.exe"))
    {
        $command = Get-Command $exe -ErrorAction SilentlyContinue

        if ($null -ne $command)
        {
            return $command
        }
    }

    $null
}



$dockerCompose = Get-Executable -name "docker-compose"
$docker = Get-Executable -name "docker"

if ($null -eq $dockerCompose)
{
    Write-Warning "Cannot locate docker-compose"
    return
}

if ($null -eq $docker)
{
    Write-Warning "Cannot locate docker"
    return
}

Push-Location ([IO.Path]::Combine($PSScriptRoot, "docker", "appveyor"))

try
{
    & $dockerCompose up -d
    Write-Host "Waiting for docker to start"
    Start-Sleep -Seconds 10

    # has a container started?
    if ($null -eq (& $docker container ls | Select-Object -Skip 1))
    {
        Write-Warning "No containers running."
        return
    }

    # Now wait for it to be up
    $start = Get-Date
    $ready = $false
    while ((Get-Date) -lt $start + [TimeSpan]::FromMinutes(2))
    {
        try
        {
            Invoke-WebRequest -UseBasicParsing -Uri http://localhost:8080 | Out-Null
            Write-Host "LocalStack container is up!"
            $ready = $true
            break
        }
        catch
        {
        }

        Start-Sleep -Seconds 2
    }

    if (-not $ready)
    {
        Write-Warning "Container did not start after 2 min"
    }
}
catch
{
    Write-Warning "Could not start container: $($_.Exception.Message)"
}

Pop-Location
