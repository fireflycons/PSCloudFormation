namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    using FluentAssertions;

    using Xunit;

    public class TerraformAwsSchema : IClassFixture<TerraformAwsSchemaFixture>
    {
        private TerraformAwsSchemaFixture fixture;

        public TerraformAwsSchema(TerraformAwsSchemaFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("aws_default_route_table", "route")]
        [InlineData("aws_default_security_group", "egress")]
        [InlineData("aws_default_security_group", "ingress")]
        [InlineData("aws_emr_cluster", "step")]
        [InlineData("aws_network_acl", "egress")]
        [InlineData("aws_network_acl", "ingress")]
        [InlineData("aws_route_table", "route")]
        [InlineData("aws_security_group", "egress")]
        [InlineData("aws_security_group", "ingress")]
        public void AssertAttributeAsBlocksWillBeRenderedAsBlocks(string resourceType, string attributeName)
        {
            var resource = this.fixture.Schema.GetResourceSchema(resourceType);
            var attribute = resource.GetAttributeByPath(attributeName);

            attribute.IsBlock.Should().BeTrue();
        }

        [Fact]
        public void AssertCanGetDeeplyNestedAttributeByPath()
        {
            Action action = () => this.fixture.KinesisAnalyticsV2Application.GetAttributeByPath(
                "application_configuration.0.application_code_configuration.#.code_content.*.text_content");

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
            this.fixture.Ec2Instance.GetAttributeByPath(attributePath).IsBlock.Should().BeTrue();
        }

        [Fact]
        public void AssertGetResourceSchemaWithUnknownAwsTypeThrows()
        {
            const string UnknownAwsType = "AWS::EC2::FooBar";

            var expectedMessage =
                $"Resource \"{UnknownAwsType}\": No corresponding Terraform resource found. If this is incorrect, please raise an issue.";
            Action action = () => this.fixture.Schema.GetResourceSchema(UnknownAwsType);

            action.Should().Throw<KeyNotFoundException>().WithMessage(expectedMessage);
        }

        [Fact]
        public void AssertGetResourceSchemaWithValidAwsTypeDoesNotThrow()
        {
            Action action = () => this.fixture.Schema.GetResourceSchema("AWS::EC2::Instance");

            action.Should().NotThrow();
        }

        [Fact]
        public void AssertMetaArgumentIdIsRetrieved()
        {
            Action action = () => this.fixture.Ec2Instance.GetAttributeByPath("id");

            action.Should().NotThrow();
        }

        [Theory]
        [InlineData("timeouts")]
        [InlineData("timeouts.0.create")]
        [InlineData("timeouts.0.delete")]
        [InlineData("timeouts.0.update")]
        public void AssertMetaArgumentTimeoutsIsRetrieved(string attributePath)
        {
            Action action = () => this.fixture.Ec2Instance.GetAttributeByPath(attributePath);

            action.Should().NotThrow();
        }

        [Theory]
        [InlineData("egress")]
        [InlineData("ingress")]
        public void AssertSecurityGroupRulesAreListAttributes(string attributePath)
        {
            this.fixture.SecurityGroup.GetAttributeByPath(attributePath).IsListOrSet.Should().BeTrue();
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

        [Fact]
        public void InvalidListPathShouldThrow()
        {
            const string InvalidAttribute = "capacity_reservation_specification.capacity_reservation_preference";

            var expectedMessage =
                $"Invalid path \"{InvalidAttribute}\": Attribute at \"{InvalidAttribute.Split('.').First()}\" is a set or a list. List indicator ('*', '#' or integer) was expected next in path.";
            Action action = () =>
                this.fixture.Ec2Instance.GetAttributeByPath(
                    "capacity_reservation_specification.capacity_reservation_preference");

            action.Should().Throw<InvalidOperationException>().WithMessage(expectedMessage);
        }

        [Fact]
        public void InvalidPathToAttributeWithValueSchemaShouldThrow()
        {
            const string InvalidAttribute = "managed_policy_arns.max_session_duration";
            var expectedMessage = $"Resource does not contain an attribute at\"{InvalidAttribute}\".";

            var resource = this.fixture.Schema.GetResourceSchema("aws_iam_role");
            Action action = () => resource.GetAttributeByPath(InvalidAttribute);

            action.Should().Throw<KeyNotFoundException>().WithMessage(expectedMessage);
        }

        [Fact]
        public void AssertMapKeyReturnsValueTypeForKey()
        {
            const string MapValueAttribute = "tags.Name";

            var schema = this.fixture.Ec2Instance.GetAttributeByPath(MapValueAttribute);

            schema.Type.Should().Be(SchemaValueType.TypeString);
        }
    }
}