@echo off
docker-compose up -d
pause
docker-compose run powershell-test-env pwsh
