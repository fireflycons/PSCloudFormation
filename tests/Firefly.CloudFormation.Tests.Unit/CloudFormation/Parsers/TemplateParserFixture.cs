namespace Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormation.Parsers;
    using Firefly.CloudFormation.Tests.Unit.resources;

    public class TemplateParserFixture : IDisposable
    {
        public TemplateParserFixture()
        {
            var jsonParser = Firefly.CloudFormation.Parsers.TemplateParser.Create(EmbeddedResourceManager.GetResourceString("test-stack.json"));
            var yamlParser = Firefly.CloudFormation.Parsers.TemplateParser.Create(EmbeddedResourceManager.GetResourceString("test-stack.yaml"));

            this.JsonParameters = jsonParser.GetParameters().ToList();
            this.JsonTemplateDescription = jsonParser.GetTemplateDescription();

            this.JsonResources = jsonParser.GetResources();
            this.YamlResources = yamlParser.GetResources();

            this.YamlParameters = yamlParser.GetParameters().ToList();
            this.YamlTemplateDescription = yamlParser.GetTemplateDescription();

            jsonParser = Firefly.CloudFormation.Parsers.TemplateParser.Create(EmbeddedResourceManager.GetResourceString("test-nested-stack.json"));
            yamlParser = Firefly.CloudFormation.Parsers.TemplateParser.Create(EmbeddedResourceManager.GetResourceString("test-nested-stack.yaml"));

            this.JsonNestedStacks = jsonParser.GetNestedStackNames();
            this.YamlNestedStacks = yamlParser.GetNestedStackNames();
        }

        internal List<TemplateFileParameter> JsonParameters { get; }

        internal List<TemplateFileParameter> YamlParameters { get; }

        internal string JsonTemplateDescription { get; }

        internal string YamlTemplateDescription { get; }

        internal IEnumerable<string> JsonNestedStacks { get; }

        internal IEnumerable<string> YamlNestedStacks { get; }

        internal IEnumerable<TemplateResource> JsonResources { get; }

        internal IEnumerable<TemplateResource> YamlResources { get; }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}