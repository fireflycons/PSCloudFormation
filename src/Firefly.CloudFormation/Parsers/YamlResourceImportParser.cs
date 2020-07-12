namespace Firefly.CloudFormation.Parsers
{
    using System.Collections.Generic;
    using System.IO;

    using Amazon.CloudFormation.Model;

    using YamlDotNet.Serialization;

    /// <summary>
    /// Concrete resource import parser for YAML
    /// </summary>
    /// <seealso cref="ResourceImportParser" />
    internal class YamlResourceImportParser : ResourceImportParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="YamlResourceImportParser"/> class.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        public YamlResourceImportParser(string fileContent)
            : base(fileContent)
        {
        }

        /// <summary>
        /// Gets the resources to import.
        /// </summary>
        /// <returns>
        /// List of resources parsed from import file.
        /// </returns>
        public override List<ResourceToImport> GetResourcesToImport()
        {
            return new DeserializerBuilder().Build()
                .Deserialize<List<ResourceToImport>>(new StringReader(this.FileContent));
        }
    }
}