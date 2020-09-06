using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.IO;
    using System.Threading.Tasks;

    using Amazon.S3.Model;

    using Firefly.EmbeddedResourceLoader;
    using Firefly.EmbeddedResourceLoader.Materialization;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;
    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    public class PythonLambdaPackager : AutoResourceLoader, IDisposable
    {
        private readonly IPathResolver pathResolver = new TestPathResolver();

        private readonly ITestOutputHelper output;

        private readonly ListObjectsV2Response fileNotFound =
            new ListObjectsV2Response { S3Objects = new List<S3Object>() };

        [EmbeddedResource("PythonLambda", DirectoryRenames = new [] {"site_packages", "site-packages"})]
        public TempDirectory PythonEnvironment;

        public PythonLambdaPackager(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Test that when the template refers to a single python script and a dependency file is present,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackageSingleFilePythonLambdaWithDependency()
        {
            var templateDir = this.PythonEnvironment;
            var template = Path.Combine(templateDir.FullPath, "template.yaml");
            var projectId = S3Util.GenerateProjectId(template);
            var logger = new TestLogger(this.output);
            var mockSts = TestHelpers.GetSTSMock();
            var mockS3 = TestHelpers.GetS3ClientWithBucketMock();
            var mockContext = new Mock<IPSCloudFormationContext>();

            // Mock the virtualenv
            Environment.SetEnvironmentVariable("VIRTUAL_ENV", Path.Combine(templateDir.FullPath, "venv"));

            mockContext.Setup(c => c.Logger).Returns(logger);

            mockS3.SetupSequence(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default)).ReturnsAsync(
                new ListObjectsV2Response
                {
                    S3Objects = new List<S3Object>
                                        {
                                            new S3Object
                                                {
                                                    BucketName = "test-bucket", Key = $"my_lambda-{projectId}-0000.zip"
                                                }
                                        }
                }).ReturnsAsync(this.fileNotFound).ReturnsAsync(this.fileNotFound);

            mockS3.Setup(s3 => s3.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default)).ReturnsAsync(
                () =>
                {
                    var resp = new GetObjectMetadataResponse();

                    resp.Metadata.Add(S3Util.PackagerHashKey, "0");
                    return resp;
                });

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
            {
                S3 = new S3Util(
                                         mockClientFactory.Object,
                                         mockContext.Object,
                                         template,
                                         "test-bucket"),
                PathResolver = this.pathResolver,
                Logger = logger
            };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            logger.VerboseMessages.Should().Contain(
                new[] { "Adding my_lambda.py", "Adding mylibrary/__init__.py" },
                "the function itself and its dependency should be in right places in zip");
            logger.VerboseMessages.Should().NotContain(
                "*__pycache__*",
                "__pycache__ should not be included in lambda packages");
        }

        /// <summary>
        /// Test that when the template refers to directory containing more than one script and a dependency file is present,
        /// then the lambda directory content and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackageComplexPythonLambdaWithDependency()
        {
            var templateDir = this.PythonEnvironment;
            var template = Path.Combine(templateDir.FullPath, "template-complex.yaml");
            var projectId = S3Util.GenerateProjectId(template);
            var logger = new TestLogger(this.output);
            var mockSts = TestHelpers.GetSTSMock();
            var mockS3 = TestHelpers.GetS3ClientWithBucketMock();
            var mockContext = new Mock<IPSCloudFormationContext>();

            // Mock the virtualenv
            Environment.SetEnvironmentVariable("VIRTUAL_ENV", Path.Combine(templateDir.FullPath, "venv"));

            mockContext.Setup(c => c.Logger).Returns(logger);

            mockS3.SetupSequence(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default)).ReturnsAsync(
                new ListObjectsV2Response
                {
                    S3Objects = new List<S3Object>
                                        {
                                            new S3Object
                                                {
                                                    BucketName = "test-bucket", Key = $"my_lambda-{projectId}-0000.zip"
                                                }
                                        }
                }).ReturnsAsync(this.fileNotFound).ReturnsAsync(this.fileNotFound);

            mockS3.Setup(s3 => s3.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default)).ReturnsAsync(
                () =>
                {
                    var resp = new GetObjectMetadataResponse();

                    resp.Metadata.Add(S3Util.PackagerHashKey, "0");
                    return resp;
                });

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
            {
                S3 = new S3Util(
                                         mockClientFactory.Object,
                                         mockContext.Object,
                                         template,
                                         "test-bucket"),
                PathResolver = this.pathResolver,
                Logger = logger
            };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            logger.VerboseMessages.Should().Contain(
                new[] { "Adding my_lambda.py", "Adding other.py", "Adding mylibrary/__init__.py" },
                "the function itself and its dependency should be in right places in zip");
            logger.VerboseMessages.Should().NotContain(
                "*__pycache__*",
                "__pycache__ should not be included in lambda packages");
        }


        public void Dispose()
        {
            this.PythonEnvironment?.Dispose();
        }
    }
}
