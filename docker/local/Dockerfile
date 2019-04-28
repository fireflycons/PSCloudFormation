FROM microsoft/powershell
RUN apt update -y && apt install cifs-utils git -y && pwsh -Command "Install-Module AWSPowerShell.netcore -Scope CurrentUser -Force"
