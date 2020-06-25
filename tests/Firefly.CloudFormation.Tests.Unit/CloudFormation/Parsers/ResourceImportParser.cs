namespace Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers
{
    using System.Collections.Generic;
    using System.Linq;

    using Amazon.CloudFormation.Model;

    using FluentAssertions;

    using Xunit;

    /// <summary>
    /// Tests the resource import parser
    /// </summary>
    /// <seealso cref="Xunit.IClassFixture{Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers.ResourceImportParserFixture}" />
    public class ResourceImportParser : IClassFixture<ResourceImportParserFixture>
    {
        /// <summary>
        /// The fixture
        /// </summary>
        private readonly ResourceImportParserFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceImportParser"/> class.
        /// </summary>
        /// <param name="fixture">The fixture.</param>
        public ResourceImportParser(ResourceImportParserFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        public List<ResourceToImport> Resources { get; set; }

        /// <summary>
        /// Test that the two resources are of the expected type
        /// </summary>
        /// <param name="format">The format.</param>
        [Theory]
        [InlineData("JSON")]
        [InlineData("YAML")]
        public void ShouldContainBucketResourceAndSecurityGroupResource(string format)
        {
            this.Arrange(format);
            this.Resources.Select(r => r.ResourceType).Should()
                .Contain(new[] { "AWS::S3::Bucket", "AWS::EC2::SecurityGroup" });
        }

        /// <summary>
        /// Test that 2 resources are read.
        /// </summary>
        /// <param name="format">The format.</param>
        [Theory]
        [InlineData("JSON")]
        [InlineData("YAML")]
        public void ShouldReadTwoResources(string format)
        {
            this.Arrange(format);
            this.Resources.Count.Should().Be(2);
        }

        /// <summary>
        /// Arranges test for the specified file format.
        /// </summary>
        /// <param name="format">The format.</param>
        private void Arrange(string format)
        {
            switch (format)
            {
                case "JSON":

                    this.Resources = this.fixture.JsonResources;
                    break;

                case "YAML":

                    this.Resources = this.fixture.YamlResources;
                    break;
            }
        }
    }
}