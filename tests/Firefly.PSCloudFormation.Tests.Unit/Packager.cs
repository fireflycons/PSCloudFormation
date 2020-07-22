using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.IO;
    using System.Threading.Tasks;

    using Amazon.S3.Model;

    using Firefly.PSCloudFormation.Tests.Unit.Resources;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;
    using Firefly.PSCloudFormation.Utils;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    public class Packager
    {
        private readonly IPathResolver pathResolver = new TestPathResolver();

        private readonly ITestOutputHelper output;

        private readonly ListObjectsV2Response fileNotFound =
            new ListObjectsV2Response { S3Objects = new List<S3Object>() };

        public Packager(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task ShouldUploadAllArtifactsWhenNoneExistInS3()
        {
            var logger = new TestLogger(this.output);
            var mockSts = TestHelpers.GetSTSMock();
            var mockS3 = TestHelpers.GetS3ClientWithBucketMock();

            mockS3.Setup(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default))
                .ReturnsAsync(this.fileNotFound);

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            var mockContext = new Mock<IPSCloudFormationContext>();
            mockContext.Setup(c => c.Logger).Returns(logger);

            using var templateDir = EmbeddedResourceManager.GetResourceDirectory("DeepNestedStack");
            using var workingDirectory = new TempDirectory();

            var template = Path.Combine(templateDir, "base-stack.json");

            var newPackage = new NewPackageCommand
                                 {
                                     S3 =
                                         new S3Util(
                                             mockClientFactory.Object,
                                             mockContext.Object,
                                             template,
                                             "test-bucket"),
                                     PathResolver = this.pathResolver,
                                     Logger = logger
                                 };

            var outputTemplatePath = await newPackage.ProcessTemplate(template, workingDirectory);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldUploadNewVersionOfArtifactWhenHashesDontMatch()
        {
            using var templateDir = EmbeddedResourceManager.GetResourceDirectory("DeepNestedStack");
            var template = Path.Combine(templateDir, "base-stack.json");
            var projectId = S3Util.GenerateProjectId(template);

            var logger = new TestLogger(this.output);
            var mockSts = TestHelpers.GetSTSMock();
            var mockS3 = TestHelpers.GetS3ClientWithBucketMock();

            mockS3.Setup(s3 => s3.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default)).ReturnsAsync(
                () =>
                    {
                        var resp = new GetObjectMetadataResponse();

                        resp.Metadata.Add(S3Util.PackagerHashKey, "0");
                        return resp;
                    });

            var mockContext = new Mock<IPSCloudFormationContext>();
            mockContext.Setup(c => c.Logger).Returns(logger);

            mockS3.SetupSequence(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default)).ReturnsAsync(
                new ListObjectsV2Response
                    {
                        S3Objects = new List<S3Object>
                                        {
                                            new S3Object
                                                {
                                                    BucketName = "test-bucket",
                                                    Key = $"lambdacomplex-{projectId}-0000.zip"
                                                }
                                        }
                    }).ReturnsAsync(this.fileNotFound).ReturnsAsync(this.fileNotFound);

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var workingDirectory = new TempDirectory();


            var newPackage = new NewPackageCommand
                                 {
                                     S3 =
                                         new S3Util(
                                             mockClientFactory.Object,
                                             mockContext.Object,
                                             template,
                                             "test-bucket"),
                                     PathResolver = this.pathResolver,
                                     Logger = logger
                                 };

            var outputTemplatePath = await newPackage.ProcessTemplate(template, workingDirectory);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldNotUploadNewVersionOfTemplateArtifactWhenHashesMatch()
        {
            // Hash of sub-nested-2.json _after_ it has been modified by packager routine.
            // This must be updated if the content of sub-nested-2.json is changed
            const string HashSubNested2AfterModification = "c0247137186e4374beea9f3faaf5a79a";

            using var templateDir = EmbeddedResourceManager.GetResourceDirectory("DeepNestedStack");
            var template = Path.Combine(templateDir, "base-stack.json");
            var projectId = S3Util.GenerateProjectId(template);
            var logger = new TestLogger(this.output);
            var mockSts = TestHelpers.GetSTSMock();
            var mockS3 = TestHelpers.GetS3ClientWithBucketMock();
            var mockContext = new Mock<IPSCloudFormationContext>();
            mockContext.Setup(c => c.Logger).Returns(logger);

            mockS3.SetupSequence(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default))
                .ReturnsAsync(this.fileNotFound)
                .ReturnsAsync(
                new ListObjectsV2Response
                {
                    S3Objects = new List<S3Object>
                                        {
                                            new S3Object
                                                {
                                                    BucketName = "test-bucket",
                                                    Key = $"sub-nested-2-{projectId}-0000.json"
                                                }
                                        }
                }).ReturnsAsync(this.fileNotFound);

            mockS3.Setup(s3 => s3.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default)).ReturnsAsync(
                () =>
                    {
                        var resp = new GetObjectMetadataResponse();

                        resp.Metadata.Add(S3Util.PackagerHashKey, HashSubNested2AfterModification);
                        return resp;
                    });

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
            {
                S3 =
                                         new S3Util(
                                             mockClientFactory.Object,
                                             mockContext.Object,
                                             template,
                                             "test-bucket"),
                PathResolver = this.pathResolver,
                Logger = logger
            };

            var outputTemplatePath = await newPackage.ProcessTemplate(template, workingDirectory);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(2));
        }

        [Fact]
        public async Task ShouldNotUploadNewVersionOfDirectoryArtifactWhenHashesMatch()
        {
            // Hash of lambda directory content before zipping
            // Zips are not idempotent - fields e.g. timestamps in central directory change with successive zips of the same content.
            const string HashDirectory = "dbc9a956ab5781ed5484781e9119ddc2";

            using var templateDir = EmbeddedResourceManager.GetResourceDirectory("DeepNestedStack");
            var template = Path.Combine(templateDir, "base-stack.json");
            var projectId = S3Util.GenerateProjectId(template);
            var logger = new TestLogger(this.output);
            var mockSts = TestHelpers.GetSTSMock();
            var mockS3 = TestHelpers.GetS3ClientWithBucketMock();
            var mockContext = new Mock<IPSCloudFormationContext>();
            mockContext.Setup(c => c.Logger).Returns(logger);

            mockS3.SetupSequence(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default)).ReturnsAsync(
                new ListObjectsV2Response
                    {
                        S3Objects = new List<S3Object>
                                        {
                                            new S3Object
                                                {
                                                    BucketName = "test-bucket",
                                                    Key = $"lambdacomplex-{projectId}-0000.zip"
                                                }
                                        }
                    }).ReturnsAsync(this.fileNotFound).ReturnsAsync(this.fileNotFound);

            mockS3.Setup(s3 => s3.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default)).ReturnsAsync(
                () =>
                {
                    var resp = new GetObjectMetadataResponse();

                    resp.Metadata.Add(S3Util.PackagerHashKey, HashDirectory);
                    return resp;
                });

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
            {
                S3 =
                                         new S3Util(
                                             mockClientFactory.Object,
                                             mockContext.Object,
                                             template,
                                             "test-bucket"),
                PathResolver = this.pathResolver,
                Logger = logger
            };

            var outputTemplatePath = await newPackage.ProcessTemplate(template, workingDirectory);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(2));
        }

    }
}
