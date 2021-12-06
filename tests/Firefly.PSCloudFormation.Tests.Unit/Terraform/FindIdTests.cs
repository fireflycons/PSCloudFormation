namespace Firefly.PSCloudFormation.Tests.Unit.Terraform
{
    using Firefly.CloudFormationParser;
    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.State;

    using FluentAssertions;

    using Moq;

    using Newtonsoft.Json.Linq;

    using Xunit;

    public class FindIdTests : AutoResourceLoader
    {
        [EmbeddedResource("FindId_Attributes.json")]
        private static JObject resourceAttributes;

        [Fact]
        public void WhenInputParameterIsListAndOneElementMatchesThenElementIsFound()
        {
            var resource = new StateFileResourceInstance { Attributes = resourceAttributes };
            var templateParameter = new Mock<IParameter>();

            templateParameter.Setup(p => p.Name).Returns("PrivateSubnets");
            templateParameter.Setup(p => p.Type).Returns("List<AWS::EC2::Subnet::Id>");
            templateParameter.Setup(p => p.GetCurrentValue()).Returns("subnet-00000000,subnet-11111111");

            var inputVariable = InputVariable.CreateParameter(templateParameter.Object);
            var result = resource.FindId(inputVariable, false);

            result.Should().HaveCount(2, "there should be 1 property match and 1 array match");
        }

        [Fact]
        public void WhenInputParameterIsStringAndAttributeArrayValueMatchesThenElementIsFound()
        {
            var resource = new StateFileResourceInstance { Attributes = resourceAttributes };
            var templateParameter = new Mock<IParameter>();

            templateParameter.Setup(p => p.Name).Returns("VpcId");
            templateParameter.Setup(p => p.Type).Returns("AWS::EC2::SecurityGroup::Id");
            templateParameter.Setup(p => p.GetCurrentValue()).Returns("sg-00000000");

            var inputVariable = InputVariable.CreateParameter(templateParameter.Object);

            var result = resource.FindId(inputVariable, false);

            result.Should().HaveCount(1).And
                .AllBeOfType<JArray>("array returned contains the element we are looking for");
        }

        [Fact]
        public void WhenInputParameterIsStringAndAttributeScalarValueMatchesThenElementIsFound()
        {
            var resource = new StateFileResourceInstance { Attributes = resourceAttributes };
            var templateParameter = new Mock<IParameter>();

            templateParameter.Setup(p => p.Name).Returns("VpcId");
            templateParameter.Setup(p => p.Type).Returns("AWS::EC2::VPC::Id");
            templateParameter.Setup(p => p.GetCurrentValue()).Returns("vpc-00000000");

            var inputVariable = InputVariable.CreateParameter(templateParameter.Object);

            var result = resource.FindId(inputVariable, false);

            result.Should().HaveCount(1).And
                .AllBeOfType<JProperty>("property returned has the element we are looking for as its value");
        }
    }
}