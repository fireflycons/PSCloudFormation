#Requires -Version 5

if ($PSVersionTable.PSEdition -ieq 'Core')
{
    if (-not (Get-Module -Name AWSPowerShell.NetCore))
    {
        Import-Module AWSPowerShell.NetCore
    }
}
else
{
    if (-not (Get-Module -Name AWSPowerShell))
    {
        Import-Module AWSPowerShell
    }
}

. (Join-Path $global:TestRoot MockS3.class.ps1)

$thisFile = $MyInvocation.MyCommand.Path

Describe 'MockS3' {

    $mockS3 = [MockS3]::UseS3Mocks()

    Context 'Bucket' {

        It 'Should create buckets' {

            New-S3Bucket -BucketName 'test-bucket'
            New-S3Bucket -BucketName 'test-bucket2'

            Test-Path -Path testdrive:\test-bucket -PathType Container | Should -Be $true
            Test-Path -Path testdrive:\test-bucket2 -PathType Container | Should -Be $true
        }

        It 'Should fail if bucket exists' {

           { New-S3Bucket -BucketName 'test-bucket' } | Should -Throw
        }

        It 'Should list existing buckets' {

            Get-S3Bucket | Should -HaveCount 2
        }

        It 'Should get named bucket' {

            Get-S3Bucket -BucketName test-bucket | Should -HaveCount 1
        }

        It 'Should add bucket tags' {

            Write-S3BucketTagging -BucketName test-bucket -TagSet @(
                @{
                    Key = 'tag1'
                    Value = 'value1'
                }
                @{
                    Key = 'tag2'
                    Value = 'value2'
                }
            )
        }

        It 'Should read bucket tags' {

            # Read back tags written above
            $tags = Get-S3BucketTagging -BucketName test-bucket
            $tags | Should -HaveCount 2
            $tags | Where-Object { $_.Key -eq 'tag1' } | Select-Object -ExpandProperty Value | Should -Be 'value1'
            $tags | Where-Object { $_.Key -eq 'tag2' } | Select-Object -ExpandProperty Value | Should -Be 'value2'
        }
    }

    Context 'PutObject/GetObject' {

        # Testdrive context has been reset
        New-S3Bucket -BucketName 'test-bucket'

        It 'Should put object' {

            Write-S3Object -BucketName test-bucket -Key test1.ps1 -File $thisFile
            Test-Path -Path TestDrive:\test-bucket\test1.ps1 | Should -Be $true
            Test-Path -Path TestDrive:\test-bucket\test1.ps1.metadata | Should -Be $true
        }

        It 'Should return all items in bucket and ignore metadata' {

            $objects = Get-S3Object -BucketName test-bucket -KeyPrefix /
            $objects | Should -HaveCount 1
        }

        It 'Should return items matching key prefix' {

            $objects = Get-S3Object -BucketName test-bucket -KeyPrefix test
            $objects | Should -HaveCount 1
        }

        It 'Should put object with metadata' {

            Write-S3Object -BucketName test-bucket -Key test2.ps1 -File $thisFile -Metadata @{ data1 = 'test1'; data2 = 'test2' }
            Test-Path -Path TestDrive:\test-bucket\test2.ps1.metadata | Should -Be $true
            $md = Get-Content -Raw TestDrive:\test-bucket\test2.ps1.metadata | ConvertFrom-Json
            $md.Metadata.data1 | Should -Be 'test1'
            $md.Metadata.data2 | Should -Be 'test2'
        }

        It 'Should get object metadata' {

            $response = Get-S3ObjectMetadata -BucketName test-bucket -Key test2.ps1
            $response.Metadata['x-amz-meta-data1'] | Should -Be 'test1'
            $response.Metadata['x-amz-meta-data2'] | Should -Be 'test2'
        }
    }
}
