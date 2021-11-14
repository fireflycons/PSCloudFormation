namespace Firefly.PSCloudFormation.Tests.Unit
{
    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Xunit;

    using se = Firefly.PSCloudFormation.Utils.StringExtensions;

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
    }
}