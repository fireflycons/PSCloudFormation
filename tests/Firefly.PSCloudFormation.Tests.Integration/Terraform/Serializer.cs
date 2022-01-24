namespace Firefly.PSCloudFormation.Tests.Integration.Terraform
{
    using System;
    using System.Collections.Generic;

    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    using FluentAssertions;

    using Moq;

    using Newtonsoft.Json.Linq;

    using Xunit;

    public class Serializer
    {
        [Fact]
        public void ShouldSerializeAwsInstance()
        {
            var events = new List<MappingKey>();

            var attributes = JObject.Parse(ResourceLoader.GetStringResource("aws_instance_attributes.json"));
            var emitter = new Mock<IHclEmitter>();

            emitter.Setup(e => e.Emit(It.IsAny<HclEvent>())).Callback((HclEvent e) =>
                {
                    if (e is MappingKey mk)
                    {
                        events.Add(mk);
                    }
                });

            Action action = () =>
                new StateFileSerializer(emitter.Object).Serialize("aws_instance", "test_instance", attributes);

            action.Should().NotThrow();
        }

        [Fact]
        public void ShouldSerializeAwsIamRole()
        {
            var events = new List<MappingKey>();

            var attributes = JObject.Parse(ResourceLoader.GetStringResource("aws_iam_role_attributes.json"));
            var emitter = new Mock<IHclEmitter>();

            emitter.Setup(e => e.Emit(It.IsAny<HclEvent>())).Callback((HclEvent e) =>
                {
                    if (e is MappingKey mk)
                    {
                        events.Add(mk);
                    }
                });

            Action action = () =>
                new StateFileSerializer(emitter.Object).Serialize("aws_iam_role", "test_role", attributes);

            action.Should().NotThrow();
        }
    }
}