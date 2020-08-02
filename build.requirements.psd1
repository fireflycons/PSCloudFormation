@{
    # Some defaults for all dependencies
    PSDependOptions         = @{
        Target     = 'CurrentUser'
        Parameters = @{
            AllowClobber       = $True
            SkipPublisherCheck = $True
        }
    }

    'psake'                 = @{
        DependencyType = 'PSGalleryModule'
        Version        = '4.7.4'
        Tags           = @('Desktop', 'Core')
    }
    'BuildHelpers'          = @{
        DependencyType = 'PSGalleryModule'
        Version        = '2.0.7'
        Tags           = @('Desktop', 'Core')
    }
    'platyps'               = @{
        DependencyType = 'PSGalleryModule'
        Version        = '0.14.0'
        Tags           = 'Desktop'
    }
}