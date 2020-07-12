using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.IO;
    using System.Threading.Tasks;

    using Amazon.S3.Model;

    using Firefly.CloudFormation.S3;
    using Firefly.CloudFormation.Utils;
    using Firefly.PSCloudFormation.Tests.Unit.Resources;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    public class Packager
    {
        private readonly IPathResolver pathResolver = new TestPathResolver();

        private readonly ITestOutputHelper output;

        public Packager(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task Test()
        {
            var logger = new TestLogger(this.output);
            var mocks3 = TestHelpers.GetS3ClientWithBucketMock();
            var mockSts = TestHelpers.GetSTSMock();
            var mockContext = TestHelpers.GetContextMock(logger);

            var mockClientFactory = new Mock<IAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mocks3.Object);
            mockClientFactory.Setup(f => f.CreateSTSClient()).Returns(mockSts.Object);

            using var tempDir = EmbeddedResourceManager.GetResourceDirectory("DeepNestedStack");
            var template = Path.Combine(tempDir, "base-stack.json");

            var newPackage = new NewPackageCommand
                                 {
                                     BucketOperations =
                                         new BucketOperations(
                                             mockClientFactory.Object,
                                             mockContext.Object),
                                     PathResolver = this.pathResolver,
                                     Logger = logger
                                 };

            var outputTemplatePath = await newPackage.ProcessTemplate(template);

            this.output.WriteLine(string.Empty);
            this.output.WriteLine(File.ReadAllText(outputTemplatePath));

            // Three objects should have been uploaded to S3
            mocks3.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Exactly(3));
        }
    }
}
