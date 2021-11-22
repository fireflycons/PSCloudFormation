namespace Firefly.PSCloudFormation.Tests.Unit.Terraform
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Tests.Unit.Terraform.Emitter;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;

    using FluentAssertions;

    using Newtonsoft.Json;

    using Xunit;
    using Xunit.Abstractions;

    using he = Firefly.PSCloudFormation.Terraform.HclSerializer.HclEmitter;

    public class HclEmitter : IClassFixture<HclEmitterFixture>
    {
        private static readonly Assembly ThisAssembly = Assembly.GetCallingAssembly();

        private readonly ITestOutputHelper output;

        private readonly HclEmitterFixture fixture;

        public HclEmitter(ITestOutputHelper output, HclEmitterFixture fixture)
        {
            this.fixture = fixture;
            this.output = output;
        }

        [Theory]
        [InlineData("iam_role1.json")]
        [InlineData("iam_role2.json")]
        [InlineData("aws_apigatewayv2_stage.json")]
        [InlineData("route_table.json")]
        [InlineData("security_group.json")]
        [InlineData("aws_cloudfront_distribution.json")]
        [InlineData("aws_lb.json")]
        [InlineData("aws_lb_listener.json")]
        [InlineData("aws_ecs_task_definition.json")]
        public void ShouldSerializeResource(string stateFile)
        {
            var state = JsonConvert.DeserializeObject<StateFile>(
                (string)ResourceLoader.GetStringResource(ResourceLoader.GetResourceStream(stateFile, ThisAssembly)));

            using var sw = new StringWriter();

            var events = new List<HclEvent>();

            var emitter = new he(sw);
            var serializer = new Serializer(emitter);

            var action = new Action(() => serializer.Serialize(state));

            action.Should().NotThrow<HclSerializerException>();

            this.fixture.Validate(sw.ToString(), new TestLogger(this.output)).Should().BeTrue();
        }
    }
}