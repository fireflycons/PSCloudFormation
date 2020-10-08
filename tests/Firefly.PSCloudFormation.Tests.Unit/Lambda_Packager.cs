namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Amazon;
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
    // ReSharper disable once InconsistentNaming
    public class Lambda_Packager : AutoResourceLoader, IDisposable
    {
        /// <summary>
        /// Materialize all test lambda directory structures
        /// See https://fireflycons.github.io/Firefly-EmbeddedResourceLoader/articles/materializers.html#tempdirectory
        /// </summary>
        [EmbeddedResource(
            "LambdaDependencies",
            DirectoryRenames = new[] { "site_packages", "site-packages", "_2_7_0", "2.7.0" })]
        public TempDirectory LambdaDependencies;

        private readonly ListObjectsV2Response fileNotFound =
            new ListObjectsV2Response { S3Objects = new List<S3Object>() };

        private readonly ITestOutputHelper output;

        private readonly IPathResolver pathResolver = new TestPathResolver();

        public Lambda_Packager(ITestOutputHelper output)
        {
            this.output = output;
        }

        public IPSAwsClientFactory ClientFactory { get; set; }

        public IPSCloudFormationContext Context { get; set; }

        public TestLogger Logger { get; set; }

        public void Dispose()
        {
            this.LambdaDependencies?.Dispose();
        }

        /// <summary>
        /// Test that when the template refers to a directory and a dependency file is present,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackageNodeDirectoryLambdaWithDependency()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "NodeLambda1");
            var template = Path.Combine(templateDir, "template-complex.yaml");
            var modulesDirectory = Path.Combine(templateDir, "Lambda", "node_modules");

            this.SetupMocks(template);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                new[] { "Adding my_lambda.js", "Adding other.js", "Adding node_modules/mylibrary/libfile.js" },
                "the function itself and its dependency should be in right places in zip");

            // Check vendor directory does not exist (was temporarily created to make the package)
            Directory.Exists(modulesDirectory).Should().BeFalse("node_modules directory transient to create package");
        }

        /// <summary>
        /// Test that when the template refers to a single node script and a dependency file is present,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackageNodeSingleFileLambdaWithDependency()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "NodeLambda1");
            var template = Path.Combine(templateDir, "template.yaml");
            var modulesDirectory = Path.Combine(templateDir, "Lambda", "node_modules");

            this.SetupMocks(template);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                new[] { "Adding my_lambda.js", "Adding node_modules/mylibrary/libfile.js" },
                "the function itself and its dependency should be in right places in zip");

            // Check vendor directory does not exist (was temporarily created to make the package)
            Directory.Exists(modulesDirectory).Should().BeFalse("node_modules directory transient to create package");
        }

        /// <summary>
        /// Test that when the template refers to a single node script and a dependency file is present,
        /// and there are local dependencies in node_modules directory,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackageNodeSingleFileLambdaWithLocalAndExternalDependencies()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "NodeLambda2");
            var template = Path.Combine(templateDir, "template.yaml");
            var modulesDirectory = Path.Combine(templateDir, "Lambda", "node_modules");

            this.SetupMocks(template);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                new[]
                    {
                        "Adding my_lambda.js", "Adding node_modules/mylibrary/libfile.js",
                        "Adding node_modules/local_lib/local_lib.js"
                    },
                "the function itself and its dependency should be in right places in zip");

            // Check vendor directory does not exist (was temporarily created to make the package)
            Directory.Exists(modulesDirectory).Should().BeTrue("node_modules directory existed prior to packaging");
        }

        /// <summary>
        /// Test that when the template refers to directory containing more than one script and a dependency file is present,
        /// then the lambda directory content and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackagePythonDirectoryLambdaWithDependency()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "PythonLambda");
            var template = Path.Combine(templateDir, "template-complex.yaml");
            this.SetupMocks(template);

            // Mock the virtualenv
            Environment.SetEnvironmentVariable("VIRTUAL_ENV", Path.Combine(templateDir, "venv"));

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                new[]
                    {
                        "Adding my_lambda.py", "Adding other.py", "Adding mylibrary/__init__.py",
                        "Adding standalone_module.py"
                    },
                "the function itself and its dependency should be in right places in zip");
            this.Logger.VerboseMessages.Should().NotContain(
                "*__pycache__*",
                "__pycache__ should not be included in lambda packages");
        }

        /// <summary>
        /// Test that when the template refers to a single python script and a dependency file is present,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackagePythonSingleFileLambdaWithDependency()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "PythonLambda");
            var template = Path.Combine(templateDir, "template.yaml");
            this.SetupMocks(template);

            // Mock the virtualenv
            Environment.SetEnvironmentVariable("VIRTUAL_ENV", Path.Combine(templateDir, "venv"));

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                new[] { "Adding my_lambda.py", "Adding mylibrary/__init__.py" },
                "the function itself and its dependency should be in right places in zip");

            this.Logger.VerboseMessages.Should().NotContain("Adding other.py", "lambda is a single script");

            this.Logger.VerboseMessages.Should().NotContain(
                "*__pycache__*",
                "__pycache__ should not be included in lambda packages");
        }

        /// <summary>
        /// Test that when the template refers to a single python script and a dependency file is present,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackagePythonSingleFileLambdaWithoutDependency()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "PythonLambda");
            var template = Path.Combine(templateDir, "template.yaml");
            this.SetupMocks(template);

            // Mock the virtualenv
            Environment.SetEnvironmentVariable("VIRTUAL_ENV", Path.Combine(templateDir, "venv"));

            // Remove the materialized lambda_dependencies so this is seen as a single file lambda
            foreach(var dep in Directory.EnumerateFiles(templateDir, "lambda-dependencies.*", SearchOption.AllDirectories))
            {
                File.Delete(dep);
            }

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                "Adding my_lambda.py",
                "the function itself and its dependency should be in right places in zip");

            this.Logger.VerboseMessages.Should().NotContain(new[] { "Adding other.py", "Adding mylibrary/__init__.py" }, "lambda is a single script");

            this.Logger.VerboseMessages.Should().NotContain(
                "*__pycache__*",
                "__pycache__ should not be included in lambda packages");
        }

        /// <summary>
        /// Test that when the template refers to a directory and a dependency file is present,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackageRubyDirectoryLambdaWithDependency()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "RubyLambda1");
            var template = Path.Combine(templateDir, "template.yaml");
            var vendorDirectory = Path.Combine(templateDir, "Lambda", "vendor");

            this.SetupMocks(template);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                new[]
                    {
                        "Adding my_lambda.rb", "Adding other.rb",
                        "Adding vendor/bundle/ruby/2.7.0/cache/mylibrary/libfile.rb"
                    },
                "the function itself and its dependency should be in right places in zip");

            // Check vendor directory does not exist (was temporarily created to make the package)
            Directory.Exists(vendorDirectory).Should().BeFalse("vendor directory transient to create package");
        }

        /// <summary>
        /// Test that when the template refers to a single ruby script and a dependency file is present,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackageRubySingleFileLambdaWithDependency()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "RubyLambda1");
            var template = Path.Combine(templateDir, "template.yaml");
            var vendorDirectory = Path.Combine(templateDir, "Lambda", "vendor");

            this.SetupMocks(template);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                new[] { "Adding my_lambda.rb", "Adding vendor/bundle/ruby/2.7.0/cache/mylibrary/libfile.rb" },
                "the function itself and its dependency should be in right places in zip");

            // Check vendor directory does not exist (was temporarily created to make the package)
            Directory.Exists(vendorDirectory).Should().BeFalse("vendor directory transient to create package");
        }

        /// <summary>
        /// Test that when the template refers to a single ruby script and a dependency file is present,
        /// and there are local dependencies in vendor directory,
        /// then the script and its dependencies are correctly packaged.
        /// </summary>
        [Fact]
        public async Task ShouldCorrectlyPackageRubySingleFileLambdaWithLocalAndExternalDependencies()
        {
            var templateDir = Path.Combine(this.LambdaDependencies.FullPath, "RubyLambda2");
            var template = Path.Combine(templateDir, "template.yaml");
            var vendorDirectory = Path.Combine(templateDir, "Lambda", "vendor");

            this.SetupMocks(template);

            using var workingDirectory = new TempDirectory();

            var newPackage = new NewPackageCommand
                                 {
                                     S3 = new S3Util(this.ClientFactory, this.Context, template, "test-bucket", null, null),
                                     PathResolver = this.pathResolver,
                                     Logger = this.Logger
                                 };

            await newPackage.ProcessTemplate(template, workingDirectory.FullPath);

            // Verify by checking messages output by the zip library
            this.Logger.VerboseMessages.Should().Contain(
                new[]
                    {
                        "Adding my_lambda.rb", "Adding vendor/bundle/ruby/2.7.0/cache/mylibrary/libfile.rb",
                        "Adding vendor/bundle/ruby/2.7.0/cache/local_lib/local_lib.rb"
                    },
                "the function itself and its dependency should be in right places in zip");

            // Check vendor directory does not exist (was temporarily created to make the package)
            Directory.Exists(vendorDirectory).Should().BeTrue("vendor directory existed prior to packaging");
        }

        private TestLogger SetupMocks(string template)
        {
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

            this.ClientFactory = mockClientFactory.Object;
            this.Context = mockContext.Object;
            this.Logger = logger;
            return logger;
        }
    }
}