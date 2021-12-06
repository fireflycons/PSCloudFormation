namespace Firefly.PSCloudFormation.Tests.Unit.Terraform
{
    using System.Collections.Generic;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform;

    using FluentAssertions;

    using Moq;

    using Xunit;

    public class IntrinsicExtensionsTests
    {
        [Fact]
        public void RefToParameterRendersAsVar()
        {
            const string ParamName = "Param1";
            var expected = $"var.{ParamName}";

            var parameter = new Mock<IParameter>();
            parameter.Setup(p => p.Name).Returns(ParamName);

            var template = new Mock<ITemplate>();
            template.Setup(t => t.Parameters).Returns(new List<IParameter> { parameter.Object });

            var @ref = new RefIntrinsic(ParamName);

            @ref.Render(template.Object, null).ReferenceExpression.Should().Be(expected);
        }

        [Theory]
        [InlineData("AWS::Region", "data.aws_region.current.name")]
        [InlineData("AWS::Partition", "data.aws_partition.partition.partition")]
        [InlineData("AWS::URLSuffix", "data.aws_partition.url_suffix.dns_suffix")]
        [InlineData("AWS::AccountId", "data.aws_caller_identity.current.account_id")]
        public void RefToAwsPseudoParameterRedersAsExpectedReference(string pseudo, string reference)
        {
            var parameter = new Mock<IParameter>();
            parameter.Setup(p => p.Name).Returns(pseudo);

            var template = new Mock<ITemplate>();
            template.Setup(t => t.Parameters).Returns(new List<IParameter>());
            template.Setup(t => t.PseudoParameters).Returns(new List<IParameter> { parameter.Object });

            var @ref = new RefIntrinsic(pseudo);

            @ref.Render(template.Object, null).ReferenceExpression.Should().Be(reference);
        }

        [Fact]
        public void FindInMapWithRefAtMapNameRendersASExpectedLocalLookup()
        {
            var pseudo = "AWS::Region";
            var expected = "local.mappings[data.aws_region.current.name].TopKey.SecondKey";

            var parameter = new Mock<IParameter>();
            parameter.Setup(p => p.Name).Returns(pseudo);

            var template = new Mock<ITemplate>();
            template.Setup(t => t.Parameters).Returns(new List<IParameter>());
            template.Setup(t => t.PseudoParameters).Returns(new List<IParameter> { parameter.Object });

            var @ref = new RefIntrinsic(pseudo);

            var findInMap = new FindInMapIntrinsic(new object[] { @ref, "TopKey", "SecondKey" });

            findInMap.Render(template.Object, null).ReferenceExpression.Should().Be(expected);
        }

        [Fact]
        public void FindInMapWithRefAtTopKeyRendersASExpectedLocalLookup()
        {
            var pseudo = "AWS::AccountId";
            var expected = "local.mappings.MapName[data.aws_caller_identity.current.account_id].SecondKey";

            var parameter = new Mock<IParameter>();
            parameter.Setup(p => p.Name).Returns(pseudo);

            var template = new Mock<ITemplate>();
            template.Setup(t => t.Parameters).Returns(new List<IParameter>());
            template.Setup(t => t.PseudoParameters).Returns(new List<IParameter> { parameter.Object });

            var @ref = new RefIntrinsic(pseudo);

            var findInMap = new FindInMapIntrinsic(new object[] { "MapName", @ref, "SecondKey" });

            findInMap.Render(template.Object, null).ReferenceExpression.Should().Be(expected);
        }
    }
}