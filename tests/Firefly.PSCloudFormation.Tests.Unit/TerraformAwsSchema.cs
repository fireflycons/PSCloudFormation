namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;

    using Xunit;

    public class TerraformAwsSchema : IClassFixture<TerraformAwsSchemaFixture>
    {
        private TerraformAwsSchemaFixture fixture;

        public TerraformAwsSchema(TerraformAwsSchemaFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void GetResourceByNameShouldThrowForUnknownAwsResource()
        {
            const string AwsName = "AWS::Foo::Bar";
            var expectedMessage =
                $"Resource \"{AwsName}\": No corresponding Terraform resource found. If this is incorrect, please raise an issue.";

            Action action = () => this.fixture.Schema.GetResourceSchema(AwsName);

            action.Should().Throw<KeyNotFoundException>().WithMessage(expectedMessage);
        }

        [Fact]
        public void GetResourceByNameShouldThrowForUnknownTerraformResource()
        {
            const string TerraformName = "aws_foo_bar";
            var expectedMessage = $"Resource \"{TerraformName}\" not found.";

            Action action = () => this.fixture.Schema.GetResourceSchema(TerraformName);

            action.Should().Throw<KeyNotFoundException>().WithMessage(expectedMessage);
        }

        [Theory]
        [InlineData("ami")]
        [InlineData("arn")]
        [InlineData("associate_public_ip_address")]
        [InlineData("availability_zone")]
        [InlineData("capacity_reservation_specification.*.capacity_reservation_preference")]
        [InlineData("capacity_reservation_specification.0.capacity_reservation_target")]
        [InlineData("cpu_core_count")]
        [InlineData("cpu_threads_per_core")]
        [InlineData("credit_specification.*.cpu_credits")]
        [InlineData("disable_api_termination")]
        public void GetAttributeByPathForEC2ResourceShouldGetItsAtrtributes(string attributePath)
        {
            Action action = () => this.fixture.Ec2Instance.GetAttributeByPath(attributePath);

            action.Should().NotThrow();
        }

        [Theory]
        [InlineData("capacity_reservation_specification")]
        [InlineData("credit_specification")]
        [InlineData("ebs_block_device")]
        [InlineData("enclave_options")]
        [InlineData("ephemeral_block_device")]
        [InlineData("launch_template")]
        [InlineData("metadata_options")]
        [InlineData("network_interface")]
        [InlineData("root_block_device")]
        public void AssertEC2BlockAttributes(string attributePath)
        {
            this.fixture.Ec2Instance.GetAttributeByPath(attributePath).IsBlock().Should().BeTrue();
        }

        [Theory]
        [InlineData("egress")]
        [InlineData("ingress")]
        public void AssertSecurityGroupRulesAreNotBlockAttributes(string attributePath)
        {
            this.fixture.SecurityGroup.GetAttributeByPath(attributePath).IsBlock().Should().BeFalse();
        }

        [Theory]
        [InlineData("egress")]
        [InlineData("ingress")]
        public void AssertSecurityGroupRulesAreListAttributes(string attributePath)
        {
            this.fixture.SecurityGroup.GetAttributeByPath(attributePath).IsListOrSet().Should().BeTrue();
        }

        [Fact]
        public void InvalidListPathShouldThrow()
        {
            Action action = () => this.fixture.Ec2Instance.GetAttributeByPath("capacity_reservation_specification.capacity_reservation_preference");

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AssertCanGetDeeplyNestedAttributeByPath()
        {
            Action action = () => this.fixture.KinesisAnalyticsV2Application.GetAttributeByPath("application_configuration.0.application_code_configuration.0.code_content.0.text_content");

            action.Should().NotThrow();
        }
    }
}