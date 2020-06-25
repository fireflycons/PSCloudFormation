import-module (Join-Path $PSScriptRoot 'Firefly.PSCloudFormation.dll')

$stackName = 'test-stack-CDF37043'
#Update-PSCFNStack1 -StackName $stackName -TemplateLocation "H:\Dev\Git\PSCloudFormation\tests\Firefly.CloudFormation.Tests.Unit\resources\progetDockerCloudFormation.yaml" -Wait
#Read-Host -Prompt "Press Enter" | Out-Null
#exit 0

New-PSCFNStack1 -StackName $stackName -TemplateLocation "H:\Dev\Git\PSCloudFormation\tests\test-stack.json" -VpcCidr 10.0.0.0/16 -Wait
Update-PSCFNStack1 -StackName $stackName -UsePreviousTemplate -VpcCidr 10.1.0.0/16 -Wait -Force
Remove-PSCFNStack1 -StackName $stackName -Wait -Force
Read-Host -Prompt "Press Enter" | Out-Null
exit 0
