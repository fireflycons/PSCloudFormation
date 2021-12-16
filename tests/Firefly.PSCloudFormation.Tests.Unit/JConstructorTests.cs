namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.Terraform.State;

    using FluentAssertions;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Xunit;

    /// <summary>
    /// Tests that reference types in <c>Firefly.PSCloudFormation.Terraform.State</c> can be serialized and deserialized as JConstructor.
    /// </summary>
    public class JConstructorTests
    {
        private const string json = @"{""Property"": ""Value""}";

        [Fact]
        public void DataSourceReferenceCanBeEncodedAndDecoded()
        {
            var reference = new DataSourceReference("aws_region", "current", "name");
            var jtoken = JObject.Parse(json);

            var property = (JProperty)jtoken.SelectToken("Property")?.Parent;
            property.Value = reference.ToJConstructor();
            var token1 = JObject.Parse(jtoken.ToString(Formatting.None));

            var con = (JConstructor)token1.SelectToken("Property");
            var outReference = Reference.FromJConstructor(con);

            outReference.ReferenceExpression.Should().Be(reference.ReferenceExpression);
        }

        [Fact]
        public void DirectReferenceCanBeEncodedAndDecoded()
        {
            var reference = new DirectReference("aws_instance.my_instance");
            var jtoken = JObject.Parse(json);

            var property = (JProperty)jtoken.SelectToken("Property")?.Parent;
            property.Value = reference.ToJConstructor();
            var token1 = JObject.Parse(jtoken.ToString(Formatting.None));

            var con = (JConstructor)token1.SelectToken("Property");
            var outReference = Reference.FromJConstructor(con);

            outReference.ReferenceExpression.Should().Be(reference.ReferenceExpression);
        }

        [Fact]
        public void DirectReferenceCanConvertToJConstructorAndBack()
        {
            var reference = new DirectReference("aws_instance.my_instance");

            var jc = reference.ToJConstructor();
            var newReference = Reference.FromJConstructor(jc);

            newReference.ReferenceExpression.Should().Be(reference.ReferenceExpression);
        }

        [Fact]
        public void IndirectReferenceCanBeEncodedAndDecoded()
        {
            var reference = new IndirectReference("aws_instance.my_instance.private_ip");
            var jtoken = JObject.Parse(json);

            var property = (JProperty)jtoken.SelectToken("Property")?.Parent;
            property.Value = reference.ToJConstructor();
            var token1 = JObject.Parse(jtoken.ToString(Formatting.None));

            var con = (JConstructor)token1.SelectToken("Property");
            var outReference = Reference.FromJConstructor(con);

            outReference.ReferenceExpression.Should().Be(reference.ReferenceExpression);
        }

        [Fact]
        public void InputVarialbleReferenceCanBeEncodedAndDecoded()
        {
            var reference = new InputVariableReference("my_variable");
            var jtoken = JObject.Parse(json);

            var property = (JProperty)jtoken.SelectToken("Property")?.Parent;
            property.Value = reference.ToJConstructor();
            var token1 = JObject.Parse(jtoken.ToString(Formatting.None));

            var con = (JConstructor)token1.SelectToken("Property");
            var outReference = Reference.FromJConstructor(con);

            outReference.ReferenceExpression.Should().Be(reference.ReferenceExpression);
        }

        [Fact]
        public void MapReferenceCanBeEncodedAndDecoded()
        {
            var reference = new MapReference("map[var.my_variable].second", 2);
            var jtoken = JObject.Parse(json);

            var property = (JProperty)jtoken.SelectToken("Property")?.Parent;
            property.Value = reference.ToJConstructor();
            var token1 = JObject.Parse(jtoken.ToString(Formatting.None));

            var con = (JConstructor)token1.SelectToken("Property");
            var outReference = Reference.FromJConstructor(con);

            outReference.ReferenceExpression.Should().Be(reference.ReferenceExpression);
        }

        [Fact]
        public void FunctionReferenceCanBeEncodedAndDecoded()
        {
            var reference = new FunctionReference(
                "join",
                new object[]
                    {
                        "-", new List<object> { "a", new InputVariableReference("my_variable").ToJConstructor(), "b" }
                    });

            var jtoken = JObject.Parse(json);

            var property = (JProperty)jtoken.SelectToken("Property")?.Parent;
            property.Value = reference.ToJConstructor();
            var json1 = jtoken.ToString();
            var token1 = JObject.Parse(jtoken.ToString(Formatting.None));

            var con = (JConstructor)token1.SelectToken("Property");
            var outReference = Reference.FromJConstructor(con);

            outReference.ReferenceExpression.Should().Be(reference.ReferenceExpression);
        }

        [Fact]
        public void FunctionReferenceWithIndexCanBeEncodedAndDecoded()
        {
            var reference = new FunctionReference(
                "cidrsubnets",
                new List<object> { "10.0.0.0/20", 8, 8, 8 },
                1);

            var jtoken = JObject.Parse(json);

            var property = (JProperty)jtoken.SelectToken("Property")?.Parent;
            property.Value = reference.ToJConstructor();
            var json1 = jtoken.ToString();
            var token1 = JObject.Parse(jtoken.ToString(Formatting.None));

            var con = (JConstructor)token1.SelectToken("Property");
            var outReference = Reference.FromJConstructor(con);

            outReference.ReferenceExpression.Should().Be(reference.ReferenceExpression);
        }
    }
}