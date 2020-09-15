namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Firefly.PSCloudFormation.Tests.Unit.Utils;
    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    public class S3UtilTest
    {
        private static readonly string ProjectId;

        private static readonly string RootTemplate;

        private ITestOutputHelper output;

        static S3UtilTest()
        {
            RootTemplate = GetThisFilePath();
            ProjectId = S3Util.GenerateProjectId(RootTemplate);
        }

        public S3UtilTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task ShouldCreateNewBucketWhenBucketDoesNotExist()
        {
            var expectedBucketName = $"cf-templates-pscloudformation-{TestHelpers.RegionName}-{TestHelpers.AccountId}";

            var logger = new TestLogger(this.output);
            var mockClientFactory = new Mock<IPSAwsClientFactory>();
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(TestHelpers.GetSTSMock().Object);
            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(TestHelpers.GetS3ClientWithoutBucketMock().Object);

            var ops = new S3Util(mockClientFactory.Object, TestHelpers.GetContextMock(logger).Object);

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
            var mockClientFactory = new Mock<IPSAwsClientFactory>();
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(TestHelpers.GetSTSMock().Object);
            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(TestHelpers.GetS3ClientWithBucketMock().Object);

            var ops = new S3Util(mockClientFactory.Object, TestHelpers.GetContextMock(logger).Object);

            var bucket = await ops.GetCloudFormationBucketAsync();

            bucket.BucketName.Should().Be(expectedBucketName);
            logger.InfoMessages.Any().Should().BeFalse();
        }

        /// <summary>
        /// Gets the this file path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Path to this <c>.cs</c> file</returns>
        private static string GetThisFilePath([CallerFilePath] string path = "")
        {
            return path;
        }
    }
}