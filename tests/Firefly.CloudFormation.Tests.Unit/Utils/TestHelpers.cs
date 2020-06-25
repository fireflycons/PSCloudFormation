namespace Firefly.CloudFormation.Tests.Unit.Utils
{
    using System.Collections.Generic;

    using Amazon;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.SecurityToken;
    using Amazon.SecurityToken.Model;

    using Firefly.CloudFormation.Utils;

    using Moq;

    public static class TestHelpers
    {
        public const string AccountId = "123456789012";

        public static readonly RegionEndpoint Region = RegionEndpoint.EUWest1;

        public static readonly string RegionName = Region.SystemName;

        internal static Mock<ICloudFormationContext> GetContextMock(TestLogger logger)
        {
            var mockContext = new Mock<ICloudFormationContext>();

            mockContext.Setup(c => c.Logger).Returns(logger);
            mockContext.Setup(c => c.TimestampGenerator).Returns(new TestTimestampGenerator());
            mockContext.Setup(c => c.Region).Returns(Region);
            mockContext.Setup(c => c.AccountId).Returns(AccountId);

            return mockContext;
        }

        public static Mock<IAmazonS3> GetS3ClientWithBucketMock()
        {
            var mock = new Mock<IAmazonS3>();

            mock.Setup(s3 => s3.ListBucketsAsync(It.IsAny<ListBucketsRequest>(), default)).ReturnsAsync(
                new ListBucketsResponse
                    {
                        Buckets = new List<S3Bucket>
                                      {
                                          new S3Bucket
                                              {
                                                  BucketName = $"cf-templates-pscloudformation-{RegionName}-{AccountId}"
                                              }
                                      }
                    });

            return mock;
        }

        public static Mock<IAmazonS3> GetS3ClientWithoutBucketMock()
        {
            var mock = new Mock<IAmazonS3>();

            mock.Setup(s3 => s3.ListBucketsAsync(It.IsAny<ListBucketsRequest>(), default))
                .ReturnsAsync(new ListBucketsResponse { Buckets = new List<S3Bucket>() });

            return mock;
        }

        public static Mock<IAmazonSecurityTokenService> GetSTSMock()
        {
            var mock = new Mock<IAmazonSecurityTokenService>();

            mock.Setup(sts => sts.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), default))
                .ReturnsAsync(new GetCallerIdentityResponse() { Account = AccountId });

            return mock;
        }

        internal static Mock<IAwsClientFactory> GetClientFactoryMock()
        {
            var mock = new Mock<IAwsClientFactory>();

            mock.Setup(f => f.CreateS3Client()).Returns(GetS3ClientWithBucketMock().Object);
            mock.Setup(f => f.CreateSTSClient()).Returns(GetSTSMock().Object);

            return mock;
        }
    }
}