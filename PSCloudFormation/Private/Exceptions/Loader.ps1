$referenceAssemblies = [System.AppDomain]::CurrentDomain.GetAssemblies() |
Where-Object {
    @(
        'AWSSDK.CloudFormation.dll'
        'System.Runtime.dll'
        'netstandard.dll'
    ) -icontains ([IO.Path]::GetFileName($_.Location))
} |
Select-Object -ExpandProperty Location

@(
    'PSCloudFormation.Exceptions.CloudFormationException.cs'
) |
ForEach-Object {
    $className = [IO.Path]::GetFileNameWithoutExtension($_)

    if (-not ($className -as [type]))
    {
        Add-Type -Path (Join-Path $PSScriptRoot $_) -ReferencedAssemblies $referenceAssemblies
    }
}