using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    using Amazon.S3.Model;

    using Firefly.CloudFormation.S3;
    using Firefly.CloudFormation.Utils;
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

            using var templateDir = EmbeddedResourceManager.GetResourceDirectory("DeepNestedStack");
            using var workingDirectory = new TempDirectory();

            var template = Path.Combine(templateDir, "base-stack.json");

            var newPackage = new NewPackageCommand
                                 {
                                     S3 =
                                         new S3Util(
                                             mockClientFactory.Object,
                                             logger,
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

            mockS3.SetupSequence(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default)).ReturnsAsync(
                new ListObjectsV2Response
                    {
                        S3Objects = new List<S3Object>
                                        {
                                            new S3Object
                                                {
                                                    BucketName = "test-bucket",
                                                    Key = $"lambdacomplex-{projectId}-0000.zip",
                                                    ETag = "0000000000000000"
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
                                             logger,
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
        public async Task ShouldNotUploadNewVersionOfArtifactWhenHashesMatch()
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
                                                    Key = $"sub-nested-2-{projectId}-0000.json",
                                                    ETag = $"\"{HashSubNested2AfterModification}\""
                                                }
                                        }
                }).ReturnsAsync(this.fileNotFound);

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
            {
                S3 =
                                         new S3Util(
                                             mockClientFactory.Object,
                                             logger,
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
