namespace Firefly.PSCloudFormation.Tests.Integration
{
    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Xunit;

    public class StringExtensions
    {
        [Theory]
        [InlineData("*_arn", "execution_arn", true)]
        [InlineData("*_date", "execution_arn", false)]
        [InlineData("*_date", "create_date", true)]
        [InlineData("*_date", "last_modified_date", true)]
        [InlineData("*_date", "last_updated_date", true)]
        [InlineData("*.#.*", "ingress.#.description", true)]
        [InlineData("*.#.*", "egress.#.cidr_blocks", true)]
        public void ShouldPerformWildcardMatch(string pattern, string text, bool expectedResult)
        {
            text.IsLike(pattern).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("FooBar", "foo_bar")]
        [InlineData("DnsName", "dns_name")]
        [InlineData("DNSName", "dns_name")]
        [InlineData("AllocationId", "allocation_id")]
        public void ShouldConvertCamelCaseToSnakeCase(string text, string expectedResult)
        {
            text.CamelCaseToSnakeCase().Should().Be(expectedResult);
        }
    }
}