namespace Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers
{
    using System.Collections.Generic;

    using FluentAssertions;

    using Xunit;

    /// <summary>
    /// Test parsing of stack parameter files
    /// </summary>
    /// <seealso cref="Xunit.IClassFixture{Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers.ParameterFileParserFixture}" />
    public class ParameterFileParser : IClassFixture<ParameterFileParserFixture>
    {
        /// <summary>
        /// The fixture
        /// </summary>
        private readonly ParameterFileParserFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterFileParser"/> class.
        /// </summary>
        /// <param name="fixture">The fixture.</param>
        public ParameterFileParser(ParameterFileParserFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        private IDictionary<string, string> Parameters { get; set; }

        [Theory]
        [InlineData("JSON")]
        [InlineData("YAML")]
        public void ShouldReadParameterFile(string format)
        {
            this.Arrange(format);

            this.Parameters.Should().HaveCount(2);
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

                    this.Parameters = this.fixture.JsonParameters;
                    break;

                case "YAML":

                    this.Parameters = this.fixture.YamlParameters;
                    break;
            }
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("YAML")]
        public void ShouldHaveVpcCidrParameterWithExpectedParametersAndValues(string format)
        {
            var expectedVpc = new KeyValuePair<string, string>("VpcCidr", "10.0.0.0/16");
            var expectedSubnet = new KeyValuePair<string, string>("SubnetCidr", "10.0.0.0/24");

            this.Arrange(format);

            this.Parameters.Should().Contain(expectedVpc).And.Contain(expectedSubnet);
        }
    }
}