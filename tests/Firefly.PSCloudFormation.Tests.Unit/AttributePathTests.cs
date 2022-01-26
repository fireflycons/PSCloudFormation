namespace Firefly.PSCloudFormation.Tests.Unit
{
    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    using FluentAssertions;

    using Xunit;

    public class AttributePathTests
    {
        [Fact]
        public void ShouldSplitIndexedPath()
        {
            const string Path = "key1['multi.part.key']";
            var expected = new[] { "key1", "multi.part.key" };

            AttributePath.Split(Path).Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ShouldSplitNormalPath()
        {
            const string Path = "key1.0.key2";

            AttributePath.Split(Path).Should().BeEquivalentTo(Path.Split('.'));
        }
    }
}