# Generic module deployment.
# This stuff should be moved to psake for a cleaner deployment view
# Nuget key in $ENV:NuGetApiKey

if($ENV:BHProjectName -and $ENV:BHProjectName.Count -eq 1)
{
    $modulePath = Split-Path -Parent $env:BHPSModuleManifest

    Deploy Module {

        By PSGalleryModule {
            FromSource $modulePath
            To PSGallery
            WithOptions @{
                ApiKey = $ENV:NuGetApiKey
            }
            Tagged Production
        }

        By AppVeyorModule {
            FromSource $modulePath
            To AppVeyor
            WithOptions @{
                Version = $env:APPVEYOR_BUILD_VERSION
            }
            Tagged Development
        }
    }
}
