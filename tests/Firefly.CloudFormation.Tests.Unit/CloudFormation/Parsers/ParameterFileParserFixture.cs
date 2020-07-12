using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.CloudFormation.Tests.Unit.CloudFormation.Parsers
{
    using Firefly.CloudFormation.Tests.Unit.resources;

    public class ParameterFileParserFixture : IDisposable
    {
        public ParameterFileParserFixture()
        {
            var jsonParser =
                Firefly.CloudFormation.Parsers.ParameterFileParser.CreateParser(
                    EmbeddedResourceManager.GetResourceString("parameter-file.json"));
            var yamlParser =
                Firefly.CloudFormation.Parsers.ParameterFileParser.CreateParser(
                    EmbeddedResourceManager.GetResourceString("parameter-file.yaml"));

            this.JsonParameters = jsonParser.ParseParameterFile();
            this.YamlParameters = yamlParser.ParseParameterFile();
        }

        public IDictionary<string, string> JsonParameters { get; set; }

        public IDictionary<string, string> YamlParameters { get; set; }

        public void Dispose()
        {
        }
    }
}
