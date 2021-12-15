$dotnet = Get-Command dotnet
$testsPassed = $true

try
{
    $(
        Get-ChildItem -Path (Join-Path $env:APPVEYOR_BUILD_FOLDER tests) -Filter *.Tests.Unit.csproj -Recurse
        Get-ChildItem -Path (Join-Path $env:APPVEYOR_BUILD_FOLDER tests) -Filter *.Tests.Integration.csproj -Recurse
    ) |
    Foreach-Object {

        Write-Host
        Write-Host "##" $_.BaseName

        & $dotnet test $_.FullName --test-adapter-path:. --logger:Appveyor

        if ($LASTEXITCODE -ne 0)
        {
            $testsPassed = $false
        }

    }

    if (-not $testsPassed)
    {
        throw "Some tests failed"
    }
}
catch
{
    Write-Host -ForegroundColor Red $_.Exception.Message
    exit 1
}