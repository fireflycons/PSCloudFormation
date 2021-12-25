namespace Firefly.PSCloudFormation.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;

    using Firefly.CloudFormation.Resolvers;

    using FluentAssertions;

    using Moq;

    using Xunit;

    /// <summary>
    /// Tests provisioning of dynamic parameters
    /// </summary>
    [Collection("Sequential")]
    public class ParameterBuilder : IClassFixture<ParameterBuilderFixture>
    {
        private readonly ParameterBuilderFixture fixture;

        public ParameterBuilder(ParameterBuilderFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("BareString")]
        [InlineData("DescribedString")]
        [InlineData("AllowedValuesString")]
        [InlineData("AllowedValuesNumber")]
        [InlineData("RegexString")]
        [InlineData("StringList")]
        [InlineData("BareNumber")]
        [InlineData("NumberList")]
        [InlineData("AvailabilityZone")]
        [InlineData("ListAvailabilityZone")]
        [InlineData("ImageId")]
        [InlineData("ListImageId")]
        [InlineData("InstanceId")]
        [InlineData("ListInstanceId")]
        [InlineData("KeyPair")]
        [InlineData("SecurityGroupName")]
        [InlineData("ListSecurityGroupName")]
        [InlineData("SecurityGroupId")]
        [InlineData("ListSecurityGroupId")]
        [InlineData("SubnetId")]
        [InlineData("ListSubnetId")]
        [InlineData("VolumeId")]
        [InlineData("ListVolumeId")]
        [InlineData("VpcId")]
        [InlineData("ListVpcId")]
        [InlineData("ZoneId")]
        [InlineData("ListZoneId")]
        public void ShouldCreateMandatoryParameterIfNoDefault(string parameterName)
        {
            var param = this.fixture.ParameterDictionary[parameterName];

            param.Attributes.Should().Contain(a => a.As<ParameterAttribute>().Mandatory);
        }

        [Fact]
        public void ShouldCreateTheSameParameterDictionaryForYamlEquivalentTemplate()
        {
            var templateBody = this.fixture.ParameterTestYaml;
            var mockTemplateResolver = new Mock<ITemplateResolver>();

            mockTemplateResolver.Setup(r => r.FileContent).Returns(templateBody);
            mockTemplateResolver.Setup(r => r.NoEchoParameters).Returns(new List<string>());
            var templateManager = new TemplateManager(mockTemplateResolver.Object, StackOperation.Create, null);

            var runtimeDefinedParameterDictionary = templateManager.GetStackDynamicParameters(new Dictionary<string, string>());

            runtimeDefinedParameterDictionary.Should().BeEquivalentTo(
                this.fixture.ParameterDictionary,
                options => options.ComparingByMembers<Attribute>());
        }

        [Theory]
        [InlineData("AvailabilityZone")]
        [InlineData("ListAvailabilityZone")]
        [InlineData("ImageId")]
        [InlineData("ListImageId")]
        [InlineData("InstanceId")]
        [InlineData("ListInstanceId")]
        [InlineData("KeyPair")]
        [InlineData("SecurityGroupName")]
        [InlineData("ListSecurityGroupName")]
        [InlineData("SecurityGroupId")]
        [InlineData("ListSecurityGroupId")]
        [InlineData("SubnetId")]
        [InlineData("ListSubnetId")]
        [InlineData("VolumeId")]
        [InlineData("ListVolumeId")]
        [InlineData("VpcId")]
        [InlineData("ListVpcId")]
        [InlineData("ZoneId")]
        [InlineData("ListZoneId")]
        public void ShouldCreateValidatePatternAttributeForAllAwsParameterTypes(string parameterName)
        {
            var param = this.fixture.ParameterDictionary[parameterName];

            param.Attributes.Any(a => (a as ValidatePatternAttribute) != null).Should().BeTrue();
        }

        [Theory]
        [InlineData("RegexString")]
        public void ShouldCreateValidatePatternAttributeForExplicitAllowedPattern(string parameterName)
        {
            var param = this.fixture.ParameterDictionary[parameterName];

            var attr = param.Attributes.Where(a => (a as ValidatePatternAttribute) != null)
                .Cast<ValidatePatternAttribute>().First();

            attr.RegexPattern.Should().Be(".*");
        }

        [Theory]
        [InlineData("AllowedValuesString", "One", "Two")]
        [InlineData("AllowedValuesNumber", 1, 2, 3)]
        public void ShouldCreateValidateSettributeForExplictAllowedValues(string parameterName, params object[] values)
        {
            var param = this.fixture.ParameterDictionary[parameterName];

            var attr = param.Attributes.Where(a => (a as ValidateSetAttribute) != null).Cast<ValidateSetAttribute>()
                .First();

            attr.ValidValues.Should().Equal(values.Select(v => v.ToString()));
        }

        [Theory]
        [InlineData("BareString", typeof(string))]
        [InlineData("DescribedString", typeof(string))]
        [InlineData("DefaultedString", typeof(string))]
        [InlineData("DefaultedNUmber", typeof(double))]
        [InlineData("AllowedValuesString", typeof(string))]
        [InlineData("AllowedValuesNumber", typeof(double))]
        [InlineData("RegexString", typeof(string))]
        [InlineData("StringList", typeof(string[]))]
        [InlineData("BareNumber", typeof(double))]
        [InlineData("NumberList", typeof(double[]))]
        [InlineData("AvailabilityZone", typeof(string))]
        [InlineData("ListAvailabilityZone", typeof(string[]))]
        [InlineData("ImageId", typeof(string))]
        [InlineData("ListImageId", typeof(string[]))]
        [InlineData("InstanceId", typeof(string))]
        [InlineData("ListInstanceId", typeof(string[]))]
        [InlineData("KeyPair", typeof(string))]
        [InlineData("SecurityGroupName", typeof(string))]
        [InlineData("ListSecurityGroupName", typeof(string[]))]
        [InlineData("SecurityGroupId", typeof(string))]
        [InlineData("ListSecurityGroupId", typeof(string[]))]
        [InlineData("SubnetId", typeof(string))]
        [InlineData("ListSubnetId", typeof(string[]))]
        [InlineData("VolumeId", typeof(string))]
        [InlineData("ListVolumeId", typeof(string[]))]
        [InlineData("VpcId", typeof(string))]
        [InlineData("ListVpcId", typeof(string[]))]
        [InlineData("ZoneId", typeof(string))]
        [InlineData("ListZoneId", typeof(string[]))]
        public void ShouldHaveExpectedClrTypeForAwsParameterType(string parameterName, Type expectedType)
        {
            var param = this.fixture.ParameterDictionary[parameterName];

            param.ParameterType.Should().Be(expectedType);
        }

        [Theory]
        [InlineData("DefaultedString")]
        [InlineData("DefaultedNumber")]
        public void ShouldNotCreateMandatoryParameterIfDefaultIsPresent(string parameterName)
        {
            var param = this.fixture.ParameterDictionary[parameterName];

            param.Attributes.Should().Contain(a => !a.As<ParameterAttribute>().Mandatory);
        }
    }
}