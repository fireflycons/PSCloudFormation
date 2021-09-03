using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.IO;

    using Amazon.CloudFormation.Model;

    using Firefly.EmbeddedResourceLoader;
    using Firefly.EmbeddedResourceLoader.Materialization;
    using Firefly.PSCloudFormation.Terraform;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;
    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    public class TFExporter : AutoResourceLoader
    {
        [EmbeddedResource("terraform.tfstate")]
        private string stateFile;

        private readonly ITestOutputHelper output;

        public TFExporter(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void ShouldLoadResourceMap()
        {
            var resources = new List<StackResource>
                                {
                                    new StackResource
                                        {
                                            LogicalResourceId = "MyInstance", ResourceType = "AWS::EC2::Instance", PhysicalResourceId = "i-01234567"
                                        }
                                };

            var logger = new TestLogger(this.output);

            var runner = new Mock<ITerraformRunner>();
            var settings = new Mock<ITerraformSettings>();
            var ui = new Mock<IUserInterface>();
            
            using var tempDirectory = new TempDirectory();

            settings.Setup(s => s.Runner).Returns(runner.Object);
            settings.Setup(s => s.AwsRegion).Returns("eu-west-1");
            settings.Setup(s => s.WorkspaceDirectory).Returns(tempDirectory);
            File.WriteAllText(Path.Combine(tempDirectory, "terraform.tfstate"), this.stateFile);

            var exp = new TerraformExporter(resources, new List<ParameterDeclaration>(), settings.Object, logger, ui.Object);
            exp.Export();

            File.Exists(Path.Combine(tempDirectory, "main.tf")).Should().BeTrue();
        }
    }
}
