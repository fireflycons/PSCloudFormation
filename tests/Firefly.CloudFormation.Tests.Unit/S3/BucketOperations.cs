namespace Firefly.CloudFormation.Tests.Unit.S3
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Firefly.CloudFormation.S3;
    using Firefly.CloudFormation.Tests.Unit.Utils;

    using FluentAssertions;

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
            var ops = new CloudFormationBucketOperations(
                TestHelpers.GetS3ClientWithoutBucketMock().Object,
                TestHelpers.GetSTSMock().Object,
                TestHelpers.GetContextMock(logger).Object);

            var bucket = await ops.GetCloudFormationBucketAsync();

            bucket.BucketName.Should().Be(expectedBucketName);
            logger.InfoMessages.First().Should().Be(
                $"Created S3 bucket {expectedBucketName} to store oversize templates.");
        }

        [Fact]
        public async Task ShouldReturnBucketWhenBucketExists()
        {
            var expectedBucketName = $"cf-templates-pscloudformation-{TestHelpers.RegionName}-{TestHelpers.AccountId}";

            var logger = new TestLogger(this.output);
            var ops = new CloudFormationBucketOperations(
                TestHelpers.GetS3ClientWithBucketMock().Object,
                TestHelpers.GetSTSMock().Object,
                TestHelpers.GetContextMock(logger).Object);

            var bucket = await ops.GetCloudFormationBucketAsync();

            bucket.BucketName.Should().Be(expectedBucketName);
            logger.InfoMessages.Any().Should().BeFalse();
        }

        [Fact]
        public async Task ShouldUploadTemplateToS3()
        {
            var logger = new TestLogger(this.output);
            var context = TestHelpers.GetContextMock(logger).Object;
            var ops = new CloudFormationBucketOperations(
                TestHelpers.GetS3ClientWithBucketMock().Object,
                TestHelpers.GetSTSMock().Object,
                context);

            using var file = new TempFile(51200);
            var expectedUri = new Uri(
                $"https://cf-templates-pscloudformation-{context.Region.SystemName}-{context.AccountId}.s3.amazonaws.com/20200101000000000_test-stack_template_{Path.GetFileName(file.Path)}");

            (await ops.UploadFileToS3("test-stack", file.Path, UploadFileType.Template)).Should().Be(expectedUri);
            logger.InfoMessages.First().Should().Be(
                $"Copying oversize template to https://cf-templates-pscloudformation-{context.Region.SystemName}-{context.AccountId}.s3.amazonaws.com/");
        }
    }
}