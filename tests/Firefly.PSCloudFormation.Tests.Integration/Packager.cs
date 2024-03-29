﻿namespace Firefly.PSCloudFormation.Tests.Integration
{
    #pragma warning disable 649
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Amazon;
    using Amazon.S3.Model;

    using Firefly.EmbeddedResourceLoader;
    using Firefly.EmbeddedResourceLoader.Materialization;
    using Firefly.PSCloudFormation.Tests.Common.Utils;
    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    [Collection("Sequential")]
    public class Packager : AutoResourceLoader, IDisposable
    {
        private readonly IPathResolver pathResolver = new TestPathResolver();

        private readonly ITestOutputHelper output;

        private readonly ListObjectsV2Response fileNotFound =
            new ListObjectsV2Response { S3Objects = new List<S3Object>() };

        /// <summary>
        /// Each test needs its own copy of the stack directory structure as the content is modified by the test.
        /// </summary>
        [EmbeddedResource("DeepNestedStack")]
        private TempDirectory deepNestedStack;

        [EmbeddedResource("no_package.yaml")]
        private TempFile noPackageStack;

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
            mockContext.Setup(c => c.Region).Returns(RegionEndpoint.EUWest1);

            using var workingDirectory = new TempDirectory();

            var template = Path.Combine(this.deepNestedStack, "base-stack.json");

            var packager = new PackagerUtils(
                new TestPathResolver(),
                logger,
                new S3Util(mockClientFactory.Object, mockContext.Object, template, "test-bucket", null, null),
                new OSInfo());

            var outputTemplatePath = await packager.ProcessTemplate(template, workingDirectory);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(await File.ReadAllTextAsync(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldUploadNewVersionOfArtifactWhenHashesDontMatch()
        {
            var template = Path.Combine(this.deepNestedStack, "base-stack.json");
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
            mockContext.Setup(c => c.Region).Returns(RegionEndpoint.EUWest1);

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

            var packager = new PackagerUtils(
                new TestPathResolver(),
                logger,
                new S3Util(mockClientFactory.Object, mockContext.Object, template, "test-bucket", null, null),
                new OSInfo());

            var outputTemplatePath = await packager.ProcessTemplate(template, workingDirectory);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(await File.ReadAllTextAsync(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldNotUploadNewVersionOfTemplateArtifactWhenHashesMatch()
        {
            var templateDir = this.deepNestedStack;
            var template = Path.Combine(templateDir, "base-stack.json");
            var projectId = S3Util.GenerateProjectId(template);
            var logger = new TestLogger(this.output);
            var mockSts = TestHelpers.GetSTSMock();
            var mockS3 = TestHelpers.GetS3ClientWithBucketMock();
            var mockContext = new Mock<IPSCloudFormationContext>();
            mockContext.Setup(c => c.Logger).Returns(logger);
            mockContext.Setup(c => c.Region).Returns(RegionEndpoint.EUWest1);

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

                        resp.Metadata.Add(S3Util.PackagerHashKey, GetModifiedTemplateHash());
                        return resp;
                    });

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var workingDirectory = new TempDirectory();

            var packager = new PackagerUtils(
                new TestPathResolver(),
                logger,
                new S3Util(mockClientFactory.Object, mockContext.Object, template, "test-bucket", null, null),
                new OSInfo());

            var outputTemplatePath = await packager.ProcessTemplate(template, workingDirectory);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(await File.ReadAllTextAsync(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(2));

            // Bit hacky, but we need to know the hash of the template after modification.
            // Different on Windows and Linux due to line endings.
            string GetModifiedTemplateHash()
            {
                var re = new Regex(@"sub-nested-2\.json.*Hash: (?<hash>[0-9a-f]+)");

                var logLine = logger.DebugMessages.FirstOrDefault(line => re.IsMatch(line));

                if (logLine == null)
                {
                    return "0";
                }

                var mc = re.Match(logLine);

                return mc.Groups["hash"].Value;
            }
        }

        [Fact]
        public async Task ShouldNotUploadNewVersionOfDirectoryArtifactWhenHashesMatch()
        {
            using var templateDir = this.deepNestedStack;
            // Hash of lambda directory content before zipping
            // Zips are not idempotent - fields e.g. timestamps in central directory change with successive zips of the same content.
            var directoryHash = new DirectoryInfo(Path.Combine(templateDir, "lambdacomplex")).MD5();
            var template = Path.Combine(templateDir, "base-stack.json");
            var projectId = S3Util.GenerateProjectId(template);
            var logger = new TestLogger(this.output);
            var mockSts = TestHelpers.GetSTSMock();
            var mockS3 = TestHelpers.GetS3ClientWithBucketMock();
            var mockContext = new Mock<IPSCloudFormationContext>();
            mockContext.Setup(c => c.Logger).Returns(logger);
            mockContext.Setup(c => c.Region).Returns(RegionEndpoint.EUWest1);
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

                    resp.Metadata.Add(S3Util.PackagerHashKey, directoryHash);
                    return resp;
                });

            var mockClientFactory = new Mock<IPSAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var workingDirectory = new TempDirectory();

            var packager = new PackagerUtils(
                new TestPathResolver(),
                logger,
                new S3Util(mockClientFactory.Object, mockContext.Object, template, "test-bucket", null, null),
                new OSInfo());

            var outputTemplatePath = await packager.ProcessTemplate(template, workingDirectory);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(await File.ReadAllTextAsync(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(2));
        }

        [Fact]
        public void ShouldDetectStackNeedsPackagingWhenNestedStackRefersToFile()
        {
            var templateFile = Path.Combine(this.deepNestedStack, "base-stack.json");

            new PackagerUtils(this.pathResolver, new TestLogger(this.output), null, new OSInfo()).RequiresPackaging(templateFile).Should().BeTrue();
        }

        [Fact]
        public void ShouldDetectStackNeedsPackagingWhenResourceRefersToFile()
        {
            var templateFile = Path.Combine(this.deepNestedStack, "sub-nested-2.json");

            new PackagerUtils(this.pathResolver, new TestLogger(this.output), null, new OSInfo()).RequiresPackaging(templateFile).Should().BeTrue();
        }

        [Fact]
        public void ShouldDetectStackDoesNotNeedPackagingWhenNoLocalFileReferences()
        {
            new PackagerUtils(this.pathResolver, new TestLogger(this.output), null, new OSInfo()).RequiresPackaging(this.noPackageStack).Should().BeFalse();
        }

        public void Dispose()
        {
            this.deepNestedStack?.Dispose();
            this.noPackageStack?.Dispose();
        }
    }
}
