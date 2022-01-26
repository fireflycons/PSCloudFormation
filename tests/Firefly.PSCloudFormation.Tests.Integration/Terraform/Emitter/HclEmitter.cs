namespace Firefly.PSCloudFormation.Tests.Integration.Terraform.Emitter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Tests.Common.Utils;

    using FluentAssertions;

    using Newtonsoft.Json;

    using Xunit;
    using Xunit.Abstractions;

    using he = Firefly.PSCloudFormation.Terraform.HclSerializer.HclEmitter;

    [Collection("Sequential")]
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

        /// <summary>
        /// Tests various features of the emitter such as
        /// - Block handling
        /// - Encoded JSON
        /// - Resource Traits
        /// </summary>
        /// <param name="stateFile">The state file.</param>
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
        [InlineData("aws_ecs_service.json")]
        [InlineData("aws_api_gateway_usage_plan.json")]
        public void ShouldSerializeResource(string stateFile)
        {
            this.output.WriteLine($"Serialize {stateFile}");
            var state = JsonConvert.DeserializeObject<StateFile>(
                (string)ResourceLoader.GetStringResource(ResourceLoader.GetResourceStream(stateFile, ThisAssembly)));

            using var sw = new StringWriter();

            var events = new List<HclEvent>();

            var emitter = new he(sw);
            var serializer = new StateFileSerializer(emitter);

            var action = new Action(() => serializer.Serialize(state));

            action.Should().NotThrow<HclSerializerException>();

            this.fixture.Validate(sw.ToString(), new TestLogger(this.output)).Should().BeTrue();
        }
    }
}