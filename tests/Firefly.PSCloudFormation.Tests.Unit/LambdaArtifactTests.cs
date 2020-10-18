namespace Firefly.PSCloudFormation.Tests.Unit
{
    #pragma warning disable 649

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Firefly.CloudFormation.Parsers;
    using Firefly.EmbeddedResourceLoader;
    using Firefly.EmbeddedResourceLoader.Materialization;
    using Firefly.PSCloudFormation.LambdaPackaging;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;
    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    public class LambdaArtifactTests : AutoResourceLoader, IDisposable
    {
        /// <summary>
        /// Sample Node.JS lambda
        /// </summary>
        private const string InlineNode =
            "// test\nexports.handler = function(event, context) {\n  console.log('hello')\n}";

        /// <summary>
        /// Sample Python lambda
        /// </summary>
        private const string InlinePython = "# test\ndef handler(event, _):\n    pass";

        /// <summary>
        /// Sample Ruby lambda
        /// </summary>
        private const string InlineRuby =
            "require 'json'\n\ndef handler(event:, context:)\n    { event: JSON.generate(event), context: JSON.generate(context.inspect) }\nend";

        /// <summary>
        /// The inline code property map
        /// </summary>
        private static readonly Dictionary<string, string> InlineCodePropertyMap = new Dictionary<string, string>
                                                                                       {
                                                                                           {
                                                                                               "AWS::Serverless::Function",
                                                                                               "InlineCode"
                                                                                           },
                                                                                           {
                                                                                               "AWS::Lambda::Function",
                                                                                               "Code.ZipFile"
                                                                                           }
                                                                                       };

        /// <summary>
        /// The inline code code map
        /// </summary>
        private static readonly Dictionary<string, string> InlineCodeCodeMap = new Dictionary<string, string>
                                                                                   {
                                                                                       { "nodejs10.x", InlineNode },
                                                                                       { "nodejs12.x", InlineNode },
                                                                                       { "python3.6", InlinePython },
                                                                                       { "python3.7", InlinePython },
                                                                                       { "ruby2.5", InlineRuby },
                                                                                       { "ruby2.7", InlineRuby }
                                                                                   };

        /// <summary>
        /// The output
        /// </summary>
        private readonly ITestOutputHelper output;

        /// <summary>
        /// The handler test directory
        /// </summary>
        [EmbeddedResource("LambdaHandler")]
        private TempDirectory handlerTestDirectory;

        /// <summary>
        /// The dependency files
        /// </summary>
        [EmbeddedResource("DependencyFile")]
        private TempDirectory dependencyFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaArtifactTests"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public LambdaArtifactTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.handlerTestDirectory?.Dispose();
            this.dependencyFiles?.Dispose();
        }

        [Fact]
        public async Task ShouldValidateHandlerForPreZippedLambda()
        {
            var mockS3 = new Mock<IPSS3Util>();

            var template = Path.Combine(this.handlerTestDirectory, "zipped_lambda.yaml");

            var parser = TemplateParser.Create(await File.ReadAllTextAsync(template));
            var function = parser.GetResources().FirstOrDefault(r => r.ResourceType == "AWS::Serverless::Function");

            function.Should().NotBeNull("you broke the template!");

            var artifact = new LambdaArtifact(new TestPathResolver(), function, template);
            var packager = LambdaPackager.CreatePackager(artifact, mockS3.Object, new TestLogger(this.output), new OSInfo());

            Func<Task> act = async () => { await packager.Package(null); };
            await act.Should().NotThrowAsync();
        }

        [InlineData("AWS::Lambda::Function", LambdaRuntimeType.Node, "nodejs10.x")]
        [InlineData("AWS::Lambda::Function", LambdaRuntimeType.Node, "nodejs12.x")]
        [InlineData("AWS::Lambda::Function", LambdaRuntimeType.Python, "python3.6")]
        [InlineData("AWS::Lambda::Function", LambdaRuntimeType.Python, "python3.7")]
        [InlineData("AWS::Lambda::Function", LambdaRuntimeType.Ruby, "ruby2.5")]
        [InlineData("AWS::Lambda::Function", LambdaRuntimeType.Ruby, "ruby2.7")]
        [InlineData("AWS::Serverless::Function", LambdaRuntimeType.Node, "nodejs10.x")]
        [InlineData("AWS::Serverless::Function", LambdaRuntimeType.Node, "nodejs12.x")]
        [InlineData("AWS::Serverless::Function", LambdaRuntimeType.Python, "python3.6")]
        [InlineData("AWS::Serverless::Function", LambdaRuntimeType.Python, "python3.7")]
        [InlineData("AWS::Serverless::Function", LambdaRuntimeType.Ruby, "ruby2.5")]
        [InlineData("AWS::Serverless::Function", LambdaRuntimeType.Ruby, "ruby2.7")]
        [Theory]
        internal async Task ShouldParseAndValidateInlineLambdaFunction(
            string resourceType,
            LambdaRuntimeType expectedRuntimeType,
            string runtime)
        {
            var mockS3 = new Mock<IPSS3Util>();
            var mockLambdaFunctionResource = new Mock<ITemplateResource>();

            mockLambdaFunctionResource.Setup(r => r.LogicalName).Returns("MockInlineLambda");
            mockLambdaFunctionResource.Setup(r => r.ResourceType).Returns(resourceType);
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Runtime")).Returns(runtime);
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Handler")).Returns("index.handler");
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue(InlineCodePropertyMap[resourceType]))
                .Returns(InlineCodeCodeMap[runtime]);

            var artifact = new LambdaArtifact(
                new TestPathResolver(),
                mockLambdaFunctionResource.Object,
                Directory.GetCurrentDirectory());

            // Verify parsing
            artifact.ArtifactType.Should().Be(LambdaArtifactType.Inline);
            artifact.InlineCode.Should().NotBeNull();
            artifact.RuntimeInfo.RuntimeType.Should().Be(expectedRuntimeType);
            artifact.HandlerInfo.FilePart.Should().Be("index");
            artifact.HandlerInfo.MethodPart.Should().Be("handler");

            // Verify handler check
            var packager = LambdaPackager.CreatePackager(artifact, mockS3.Object, new TestLogger(this.output), new OSInfo());
            Func<Task> act = async () => { await packager.Package(null); };

            await act.Should().NotThrowAsync();
        }

        [InlineData("AWS::Lambda::Function", "nodejs10.x")]
        [InlineData("AWS::Lambda::Function", "nodejs12.x")]
        [InlineData("AWS::Lambda::Function", "python3.6")]
        [InlineData("AWS::Lambda::Function", "python3.7")]
        [InlineData("AWS::Serverless::Function", "nodejs10.x")]
        [InlineData("AWS::Serverless::Function", "nodejs12.x")]
        [InlineData("AWS::Serverless::Function", "python3.6")]
        [InlineData("AWS::Serverless::Function", "python3.7")]
        [Theory]
        internal async Task ShouldThrowWhenInlineLambdaHasMissingHandlerMethod(string resourceType, string runtime)
        {
            var mockS3 = new Mock<IPSS3Util>();
            var mockLambdaFunctionResource = new Mock<ITemplateResource>();

            mockLambdaFunctionResource.Setup(r => r.LogicalName).Returns("MockInlineLambda");
            mockLambdaFunctionResource.Setup(r => r.ResourceType).Returns(resourceType);
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Runtime")).Returns(runtime);
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Handler"))
                .Returns("index.mistyped_handler");
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue(InlineCodePropertyMap[resourceType]))
                .Returns(InlineCodeCodeMap[runtime]);

            var artifact = new LambdaArtifact(
                new TestPathResolver(),
                mockLambdaFunctionResource.Object,
                Directory.GetCurrentDirectory());

            // Verify parsing
            var packager = LambdaPackager.CreatePackager(artifact, mockS3.Object, new TestLogger(this.output), new OSInfo());
            artifact.HandlerInfo.MethodPart.Should().Be("mistyped_handler");

            Func<Task> act = async () => { await packager.Package(null); };

            await act.Should().ThrowAsync<PackagerException>().WithMessage("*Cannot locate handler method*");
        }

        /// <summary>
        /// Can only warn when handler cannot be matched with function in Ruby as cannot check
        /// for a handler that is a class method without code to understand Ruby grammar.
        /// </summary>
        [InlineData("AWS::Lambda::Function", "Code.ZipFile")]
        [InlineData("AWS::Serverless::Function", "InlineCode")]
        [Theory]
        internal async Task ShouldWarnWhenInlineRubyLambdaHasMissingHandlerMethod(
            string resourceType,
            string codeProperty)
        {
            var logger = new TestLogger(this.output);
            var mockS3 = new Mock<IPSS3Util>();
            var mockLambdaFunctionResource = new Mock<ITemplateResource>();

            mockLambdaFunctionResource.Setup(r => r.LogicalName).Returns("MockInlineLambda");
            mockLambdaFunctionResource.Setup(r => r.ResourceType).Returns(resourceType);
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Runtime")).Returns("ruby2.7");
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Handler"))
                .Returns("index.mistyped_handler");
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue(codeProperty)).Returns(InlineRuby);

            var artifact = new LambdaArtifact(
                new TestPathResolver(),
                mockLambdaFunctionResource.Object,
                Directory.GetCurrentDirectory());

            var packager = LambdaPackager.CreatePackager(artifact, mockS3.Object, logger, new OSInfo());

            Func<Task> act = async () => { await packager.Package(null); };
            await act.Should().NotThrowAsync();

            logger.WarningMessages.Should().ContainMatch(
                "*If your method is within a class, validation is not yet supported for this.");
        }

        [Theory]
        [InlineData("json")]
        [InlineData("yaml")]
        public void ShouldLoadDependencies(string format)
        {
            var deleteMap = new Dictionary<string, string> { { "json", "yaml" }, { "yaml", "json" } };

            // dependencyFiles temp dir is initialized each test by ctor,
            // so delete the dependency file NOT being tested
            File.Delete(Path.Combine(this.dependencyFiles, $"lambda-dependencies.{deleteMap[format]}"));
            var mockLambdaFunctionResource = new Mock<ITemplateResource>();

            mockLambdaFunctionResource.Setup(r => r.LogicalName).Returns("MockInlineLambda");
            mockLambdaFunctionResource.Setup(r => r.ResourceType).Returns("AWS::Lambda::Function");
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Runtime")).Returns("python3.6");
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Code")).Returns(this.dependencyFiles);
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Handler")).Returns("index.handler");

            var artifact = new LambdaArtifact(
                new TestPathResolver(),
                mockLambdaFunctionResource.Object,
                this.dependencyFiles);

            var dependencies = artifact.LoadDependencies();
            dependencies.Count.Should().Be(2);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("yaml")]
        public void ShouldResolveRelativeDependencies(string format)
        {
            var deleteMap = new Dictionary<string, string> { { "json", "yaml" }, { "yaml", "json" } };

            // dependencyFiles temp dir is initialized each test by ctor,
            // so delete the dependency file NOT being tested
            File.Delete(Path.Combine(this.dependencyFiles, $"lambda-dependencies.{deleteMap[format]}"));
            var mockLambdaFunctionResource = new Mock<ITemplateResource>();

            mockLambdaFunctionResource.Setup(r => r.LogicalName).Returns("MockInlineLambda");
            mockLambdaFunctionResource.Setup(r => r.ResourceType).Returns("AWS::Lambda::Function");
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Runtime")).Returns("nodejs10.x");
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Code")).Returns(this.dependencyFiles);
            mockLambdaFunctionResource.Setup(r => r.GetResourcePropertyValue("Handler")).Returns("index.handler");

            var artifact = new LambdaArtifact(
                new TestPathResolver(),
                mockLambdaFunctionResource.Object,
                this.dependencyFiles);

            var dependencyFile = Path.Combine(this.dependencyFiles, $"lambda-dependencies.{format}");
            var expectedDependencyPath = Path.GetFullPath(Path.Combine(new FileInfo(dependencyFile).DirectoryName, "../modules"));

            // Last entry in the lambda-dependencies files has a relative link
            var relativeDependency = artifact.LoadDependencies().Last();

            relativeDependency.Location.Should().Be(expectedDependencyPath);
        }
    }
}