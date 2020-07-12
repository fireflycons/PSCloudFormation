namespace Firefly.CloudFormation.Tests.Unit.S3
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Firefly.CloudFormation.S3;
    using Firefly.CloudFormation.Tests.Unit.Utils;
    using Firefly.CloudFormation.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    public class BucketOperations
    {
        private readonly ITestOutputHelper output;

        public BucketOperations(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task ShouldCreateNewBucketWhenBucketDoesNotExist()
        {
            var expectedBucketName = $"cf-templates-pscloudformation-{TestHelpers.RegionName}-{TestHelpers.AccountId}";

            var logger = new TestLogger(this.output);
            var mockClientFactory = new Mock<IAwsClientFactory>();
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(TestHelpers.GetSTSMock().Object);
            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(TestHelpers.GetS3ClientWithoutBucketMock().Object);

            var ops = new Firefly.CloudFormation.S3.BucketOperations(
                mockClientFactory.Object,
                TestHelpers.GetContextMock(logger).Object);

            var bucket = await ops.GetCloudFormationBucketAsync();

            bucket.BucketName.Should().Be(expectedBucketName);
            logger.InfoMessages.First().Should().Be(
                $"Creating bucket '{expectedBucketName}' to store uploaded templates.");
        }

        [Fact]
        public async Task ShouldReturnBucketWhenBucketExists()
        {
            var expectedBucketName = $"cf-templates-pscloudformation-{TestHelpers.RegionName}-{TestHelpers.AccountId}";

            var logger = new TestLogger(this.output);
            var mockClientFactory = new Mock<IAwsClientFactory>();
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(TestHelpers.GetSTSMock().Object);
            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(TestHelpers.GetS3ClientWithBucketMock().Object);

            var ops = new Firefly.CloudFormation.S3.BucketOperations(
                mockClientFactory.Object,
                TestHelpers.GetContextMock(logger).Object);

            var bucket = await ops.GetCloudFormationBucketAsync();

            bucket.BucketName.Should().Be(expectedBucketName);
            logger.InfoMessages.Any().Should().BeFalse();
        }
    }
}