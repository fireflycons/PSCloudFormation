$isAppveyor = $null -ne $env:APPVEYOR
$isReleasePublication = $isAppveyor -and $env:APPVEYOR_REPO_BRANCH -eq "master" -and $env:APPVEYOR_REPO_TAG -ieq "true"
$canPublishDocs = $isAppveyor -and $null -ne $env:APPVEYOR_REPO_NAME -and ($env:APPVEYOR_REPO_NAME).StartsWith("fireflycons/")
$forceDocPush = -not [string]::IsNullOrEmpty($env:FORCE_DOC_PUSH)
