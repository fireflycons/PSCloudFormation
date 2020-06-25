using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers
{
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Tests.Unit.resources;

    public class ResourceImportParserFixture : IDisposable
    {
        public ResourceImportParserFixture()
        {
            var jsonParser = Firefly.CloudFormation.CloudFormation.ResourceImportParser.CreateParser(EmbeddedResourceManager.GetResourceString("test-imports.json"));
            var yamlParser = Firefly.CloudFormation.CloudFormation.ResourceImportParser.CreateParser(EmbeddedResourceManager.GetResourceString("test-imports.yaml"));

            this.JsonResources = jsonParser.GetResourcesToImport();
            this.YamlResources = yamlParser.GetResourcesToImport();
        }

        internal List<ResourceToImport> JsonResources { get; }

        internal List<ResourceToImport> YamlResources { get; }


        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
