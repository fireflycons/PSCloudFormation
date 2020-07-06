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
            var parser = Firefly.CloudFormation.CloudFormation.Parsers.TemplateParser.CreateParser(
                EmbeddedResourceManager.GetResourceString("test-resource-update.json"));
            var resources = parser.GetResources();

            var resource = resources.First(r => r.LogicalName == "MyJob");

            // resource.UpdateResourceProperty("Code", new { S3Bucket = "bucket-name", S3Key = "code/lambda.zip" });
            resource.UpdateResourceProperty("Command/ScriptLocation", "s3://bucket/job.etl");
            var modifiedTemplate = parser.GetTemplate();

            modifiedTemplate.Should().Contain("\"ScriptLocation\": \"s3://bucket/job.etl\"");
        }

        [Fact]
        public void UpdateYamlResource()
        {
            var parser = Firefly.CloudFormation.CloudFormation.Parsers.TemplateParser.CreateParser(
                EmbeddedResourceManager.GetResourceString("test-resource-update.yaml"));
            var resources = parser.GetResources();

            var resource = resources.First(r => r.LogicalName == "lambdaFunction");

            resource.UpdateResourceProperty("Code", new { S3Bucket = "bucket-name", S3Key = "code/lambda.zip" });
            var modifiedTemplate = parser.GetTemplate();
            modifiedTemplate.Should().Contain("S3Bucket: bucket-name").And.Contain("S3Key: code/lambda.zip");
        }
    }
}