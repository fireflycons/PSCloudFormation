namespace Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormation.Parsers;

    using FluentAssertions;

    using Xunit;

    /// <summary>
    /// Test the template parser
    /// </summary>
    /// <seealso cref="Xunit.IClassFixture{TemplateParserFixture}" />
    public class TemplateParser : IClassFixture<TemplateParserFixture>
    {
        /// <summary>
        /// Constant 'JSON'
        /// </summary>
        private const string Json = "JSON";

        /// <summary>
        /// Constant 'YAML'
        /// </summary>
        private const string Yaml = "YAML";

        /// <summary>
        /// The fixture
        /// </summary>
        private readonly TemplateParserFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateParser"/> class.
        /// </summary>
        /// <param name="fixture">The fixture.</param>
        public TemplateParser(TemplateParserFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Gets the expected template description.
        /// </summary>
        /// <value>
        /// The expected template description.
        /// </value>
        public string ExpectedTemplateDescription { get; private set; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public List<TemplateFileParameter> Parameters { get; private set; }

        /// <summary>
        /// Gets the resources.
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        public IEnumerable<TemplateResource> Resources { get; private set; }

        /// <summary>
        /// Gets the template description.
        /// </summary>
        /// <value>
        /// The template description.
        /// </value>
        public string TemplateDescription { get; private set; }

        /// <summary>
        /// Gets the nested stacks.
        /// </summary>
        /// <value>
        /// The nested stacks.
        /// </value>
        public List<string> NestedStacks { get; private set; }

        /// <summary>
        /// Template should have two parameters.
        /// </summary>
        /// <param name="format">The format.</param>
        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void ShouldHaveTwoParameters(string format)
        {
            this.Arrange(format);
            this.Parameters.Count().Should().Be(2);
        }

        /// <summary>
        /// Template should  have <c>VpcCidr</c> parameter.
        /// </summary>
        /// <param name="format">The format.</param>
        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void ShouldHaveVpcCidrParameter(string format)
        {
            this.Arrange(format);
            Action action = () => this.Parameters.First(p => p.Name == "VpcCidr");

            action.Should().NotThrow();
        }

        /// <summary>
        /// Template should have expected description.
        /// </summary>
        /// <param name="format">The format.</param>
        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void TemplateShouldHaveExpectedDescription(string format)
        {
            this.Arrange(format);
            this.TemplateDescription.Should().Be(this.ExpectedTemplateDescription);
        }

        /// <summary>
        /// <c>VpcCidr</c> parameter should be of type string.
        /// </summary>
        /// <param name="format">The format.</param>
        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void VpcCidrParameterShouldBeOfTypeString(string format)
        {
            this.Arrange(format);
            const string Expected = "String";

            this.Parameters.First(p => p.Name == "VpcCidr").Type.Should().Be(Expected);
        }

        /// <summary>
        /// <c>VpcCidr</c> parameter should have allowed pattern regex.
        /// </summary>
        /// <param name="format">The format.</param>
        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void VpcCidrParameterShouldHaveAllowedPatternRegex(string format)
        {
            this.Arrange(format);
            this.Parameters.First(p => p.Name == "VpcCidr").AllowedPattern.Should().NotBeNull();
        }

        /// <summary>
        /// <c>VpcCidr</c> parameter should have expected description.
        /// </summary>
        /// <param name="format">The format.</param>
        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void VpcCidrParameterShouldHaveExpectedDescription(string format)
        {
            const string Expected = "CIDR block for VPC";

            this.Arrange(format);
            this.Parameters.First(p => p.Name == "VpcCidr").Description.Should().Be(Expected);
        }

        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void ShouldParseNestedStackNames(string format)
        {
            const string ExpectedStackName = "NestedStack";

            this.Arrange(format);
            this.NestedStacks.Count.Should().Be(1);
            this.NestedStacks.First().Should().Be(ExpectedStackName);
        }

        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void ShouldBeOneResourceInTemplate(string format)
        {
            this.Arrange(format);
            this.Resources.Count().Should().Be(1);
        }

        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void ShoudHaveVpcResource(string format)
        {
            this.Arrange(format);
            this.Resources.First().ResourceType.Should().Be("AWS::EC2::VPC");
        }

        [Theory]
        [InlineData(Json)]
        [InlineData(Yaml)]
        public void ShoudHaveResourceNamedVpc(string format)
        {
            this.Arrange(format);
            this.Resources.First().LogicalName.Should().Be("Vpc");
        }

        /// <summary>
        /// Arrange test for specified template format.
        /// </summary>
        /// <param name="format">The format.</param>
        private void Arrange(string format)
        {
            this.ExpectedTemplateDescription = $"{format} Template";

            switch (format)
            {
                case "JSON":

                    this.Parameters = this.fixture.JsonParameters;
                    this.TemplateDescription = this.fixture.JsonTemplateDescription;
                    this.NestedStacks = this.fixture.JsonNestedStacks.ToList();
                    this.Resources = this.fixture.JsonResources;
                    break;

                case "YAML":

                    this.Parameters = this.fixture.YamlParameters;
                    this.TemplateDescription = this.fixture.YamlTemplateDescription;
                    this.NestedStacks = this.fixture.YamlNestedStacks.ToList();
                    this.Resources = this.fixture.YamlResources;

                    break;
            }
        }
    }
}