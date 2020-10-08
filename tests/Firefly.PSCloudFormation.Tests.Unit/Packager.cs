using System;
using System.Collections.Generic;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    #pragma warning disable 649

    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
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

    [Collection("Packager")]
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

        [EmbeddedResource("DependencyFile")]
        private TempDirectory dependencyFiles;

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

            using var workingDirectory = new TempDirectory();

            var template = Path.Combine(this.deepNestedStack.FullPath, "base-stack.json");

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

            var outputTemplatePath = await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldUploadNewVersionOfArtifactWhenHashesDontMatch()
        {
            var template = Path.Combine(this.deepNestedStack.FullPath, "base-stack.json");
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

            var outputTemplatePath = await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldNotUploadNewVersionOfTemplateArtifactWhenHashesMatch()
        {
            var templateDir = this.deepNestedStack;
            var template = Path.Combine(templateDir.FullPath, "base-stack.json");
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

                        resp.Metadata.Add(S3Util.PackagerHashKey, GetModifiedTemplateHash(logger));
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

            var outputTemplatePath = await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(2));

            // Bit hacky, but we need to know the hash of the template after modification.
            // Different on Windows and Linux due to line endings.
            string GetModifiedTemplateHash(TestLogger logger)
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
            var directoryHash = new DirectoryInfo(Path.Combine(templateDir.FullPath, "lambdacomplex")).MD5();
            var template = Path.Combine(templateDir.FullPath, "base-stack.json");
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

                    resp.Metadata.Add(S3Util.PackagerHashKey, directoryHash);
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

            var outputTemplatePath = await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mockS3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(2));
        }

        [Theory]
        [InlineData("json")]
        [InlineData("yaml")]
        public void ShouldParsePackageDependencies(string format)
        {
            var dependencyFile = Path.Combine(this.dependencyFiles.FullPath, $"lambda-dependencies.{format}");

            var dependencies = PSCloudFormation.LambdaPackager.LoadDependencies(dependencyFile);
            dependencies.Count.Should().Be(2);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("yaml")]
        public void ShouldResolveRelativeDependencies(string format)
        {
            var dependencyFile = Path.Combine(this.dependencyFiles.FullPath, $"lambda-dependencies.{format}");
            var expectedDependencyPath = Path.GetFullPath(Path.Combine(new FileInfo(dependencyFile).DirectoryName, "../modules"));
            
            // Last entry in the lambda-dependencies files has a relative link
            var relativeDependency = LambdaPackager.LoadDependencies(dependencyFile).Last();

            relativeDependency.Location.Should().Be(expectedDependencyPath);
        }

        [Fact]
        public void ShouldDetectStackNeedsPackagingWhenNestedStackRefersToFile()
        {
            var templateFile = Path.Combine(this.deepNestedStack, "base-stack.json");
            var templateContent = File.ReadAllText(templateFile);

            new PackagerUtils(this.pathResolver).RequiresPackaging(templateContent, templateFile).Should().BeTrue();
        }

        [Fact]
        public void ShouldDetectStackNeedsPackagingWhenResourceRefersToFile()
        {
            var templateFile = Path.Combine(this.deepNestedStack, "sub-nested-2.json");
            var templateContent = File.ReadAllText(templateFile);

            new PackagerUtils(this.pathResolver).RequiresPackaging(templateContent, templateFile).Should().BeTrue();
        }

        [Fact]
        public void ShouldDetectStackDoesNotNeedPackagingWhenNoLocalFileReferences()
        {
            var templateContent = File.ReadAllText(this.noPackageStack);

            new PackagerUtils(this.pathResolver).RequiresPackaging(templateContent, this.noPackageStack).Should().BeFalse();
        }

        public void Dispose()
        {
            this.deepNestedStack?.Dispose();
            this.noPackageStack?.Dispose();
        }
    }
}
