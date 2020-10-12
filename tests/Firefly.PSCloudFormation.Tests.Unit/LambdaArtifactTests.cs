using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.IO;
    using System.Linq;
    using System.Management.Automation;

    using Firefly.CloudFormation.Parsers;
    using Firefly.EmbeddedResourceLoader;

    using FluentAssertions;

    using Xunit;

    public class LambdaArtifactTests
    {
        [Fact]
        public void ShouldParseInlineLambdaFunction()
        {
            using var template = ResourceLoader.GetFileResource("inlineHandler.yaml");

            var parser = TemplateParser.Create(File.ReadAllText(template));
            var function = parser.GetResources().FirstOrDefault(r => r.ResourceType == "AWS::Lambda::Function");

            function.Should().NotBeNull("you broke the template!");

            var artifact = new LambdaArtifact(new TestPathResolver(), template, function);

            artifact.ArtifactType.Should().Be(LambdaArtifactType.Inline);
            artifact.InlineCode.Should().NotBeNull();
            artifact.RuntimeInfo.RuntimeType.Should().Be(LambdaRuntimeType.Node);
            artifact.HandlerInfo.FilePart.Should().Be("index");
            artifact.HandlerInfo.MethodPart.Should().Be("handler");
        }

        [Fact]
        public void ShouldParseInlineServerlessFunction()
        {
            using var template = ResourceLoader.GetFileResource("inlineHandler.yaml");

            var parser = TemplateParser.Create(File.ReadAllText(template));
            var function = parser.GetResources().FirstOrDefault(r => r.ResourceType == "AWS::Serverless::Function");

            function.Should().NotBeNull("you broke the template!");

            var artifact = new LambdaArtifact(new TestPathResolver(), template, function);

            artifact.ArtifactType.Should().Be(LambdaArtifactType.Inline);
            artifact.InlineCode.Should().NotBeNull();
            artifact.RuntimeInfo.RuntimeType.Should().Be(LambdaRuntimeType.Python);
            artifact.HandlerInfo.FilePart.Should().Be("index");
            artifact.HandlerInfo.MethodPart.Should().Be("handler");
        }
    }
}
