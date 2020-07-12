namespace Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers
{
    using System.Linq;

    using Firefly.CloudFormation.Tests.Unit.resources;

    using FluentAssertions;

    using Xunit;

    /// <summary>
    /// Tests updating of resource properties within a template.
    /// You would need to do this to implement the behaviour of <c>aws cloudformation package</c>
    /// </summary>
    public class ResourcePropertyUpdate
    {
        [Fact]
        public void UpdateJsonResource()
        {
            const string S3Location = "s3://bucket/job.etl";

            var parser = Firefly.CloudFormation.Parsers.TemplateParser.Create(
                EmbeddedResourceManager.GetResourceString("test-resource-update.json"));
            var resources = parser.GetResources();

            var resource = resources.First(r => r.LogicalName == "MyJob");

            // resource.UpdateResourceProperty("Code", new { S3Bucket = "bucket-name", S3Key = "code/lambda.zip" });
            resource.UpdateResourceProperty("Command.ScriptLocation", S3Location);
            var modifiedTemplate = parser.GetTemplate();

            modifiedTemplate.Should().Contain($"\"ScriptLocation\": \"{S3Location}\"");
            resource.GetResourcePropertyValue("Command.ScriptLocation").Should().Be(S3Location);
        }

        [Fact]
        public void UpdateYamlResource()
        {
            const string S3Bucket = "bucket-name";
            const string S3Key = "code/lambda.zip";

            var parser = Firefly.CloudFormation.Parsers.TemplateParser.Create(
                EmbeddedResourceManager.GetResourceString("test-resource-update.yaml"));
            var resources = parser.GetResources();

            var resource = resources.First(r => r.LogicalName == "lambdaFunction");

            resource.UpdateResourceProperty("Code", new { S3Bucket, S3Key });
            var modifiedTemplate = parser.GetTemplate();
            modifiedTemplate.Should().Contain($"S3Bucket: {S3Bucket}").And.Contain($"S3Key: {S3Key}");
            resource.GetResourcePropertyValue("Code.S3Bucket").Should().Be(S3Bucket);
            resource.GetResourcePropertyValue("Code.S3Key").Should().Be(S3Key);
        }
    }
}