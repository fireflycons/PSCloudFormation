namespace Firefly.CloudFormation.CloudFormation
{
    using System.Collections.Generic;
    using System.IO;

    using Amazon.CloudFormation.Model;

    /// <summary>
    /// Base class for Resource Import file parsers
    /// </summary>
    /// <seealso cref="Firefly.CloudFormation.CloudFormation.InputFileParser" />
    public abstract class ResourceImportParser : InputFileParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceImportParser"/> class.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        protected ResourceImportParser(string fileContent)
            : base(fileContent)
        {
        }

        /// <summary>
        /// Creates the parser.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        /// <returns>A new <see cref="ResourceImportParser"/>.</returns>
        /// <exception cref="InvalidDataException">Resource import file is empty</exception>
        public static ResourceImportParser CreateParser(string fileContent)
        {
            switch (InputFileParser.GetInputFileFormat(fileContent))
            {
                case InputFileFormat.Json:

                    return new JsonResourceImportParser(fileContent);

                case InputFileFormat.Yaml:

                    return new YamlResourceImportParser(fileContent);

                default:

                    throw new InvalidDataException("Resource import file is empty");
            }
        }

        /// <summary>
        /// Gets the resources to import.
        /// </summary>
        /// <returns>List of resources parsed from import file.</returns>
        public abstract List<ResourceToImport> GetResourcesToImport();
    }
}