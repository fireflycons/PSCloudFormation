﻿namespace Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormation.CloudFormation.Template;
    using Firefly.CloudFormation.Tests.Unit.resources;

    public class TemplateParserFixture : IDisposable
    {
        public TemplateParserFixture()
        {
            var jsonParser = Firefly.CloudFormation.CloudFormation.Template.TemplateParser.CreateParser(EmbeddedResourceManager.GetResourceString("test-stack.json"));
            var yamlParser = Firefly.CloudFormation.CloudFormation.Template.TemplateParser.CreateParser(EmbeddedResourceManager.GetResourceString("test-stack.yaml"));

            this.JsonParameters = jsonParser.GetParameters().ToList();
            this.JsonTemplateDescription = jsonParser.GetTemplateDescription();

            this.YamlParameters = yamlParser.GetParameters().ToList();
            this.YamlTemplateDescription = yamlParser.GetTemplateDescription();

            jsonParser = Firefly.CloudFormation.CloudFormation.Template.TemplateParser.CreateParser(EmbeddedResourceManager.GetResourceString("test-nested-stack.json"));
            yamlParser = Firefly.CloudFormation.CloudFormation.Template.TemplateParser.CreateParser(EmbeddedResourceManager.GetResourceString("test-nested-stack.yaml"));

            this.JsonNestedStacks = jsonParser.GetNestedStackNames();
            this.YamlNestedStacks = yamlParser.GetNestedStackNames();
        }

        internal List<TemplateFileParameter> JsonParameters { get; }

        internal List<TemplateFileParameter> YamlParameters { get; }

        internal string JsonTemplateDescription { get; }

        internal string YamlTemplateDescription { get; }

        internal IEnumerable<string> JsonNestedStacks { get; }

        internal IEnumerable<string> YamlNestedStacks { get; }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}