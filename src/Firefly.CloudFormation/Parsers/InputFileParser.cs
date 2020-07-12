namespace Firefly.CloudFormation.Parsers
{
    using Firefly.CloudFormation.Model;

    /// <summary>
    /// Base class for all YAML/JSON parser types
    /// </summary>
    public abstract class InputFileParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputFileParser"/> class.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        protected InputFileParser(string fileContent)
        {
            this.FileContent = fileContent;
        }

        /// <summary>
        /// Gets the template body.
        /// </summary>
        /// <value>
        /// The template body.
        /// </value>
        protected string FileContent { get; }

        /// <summary>
        /// Gets the input file format.
        /// </summary>
        /// <param name="fileContent">The file content to guess the format of.</param>
        /// <returns>The input file format</returns>
        protected static SerializationFormat GetInputFileFormat(string fileContent)
        {
            var body = fileContent.Trim();

            if (body.Length == 0)
            {
                return SerializationFormat.Unknown;
            }

            return body[0] == '[' || body[0] == '{' ? SerializationFormat.Json : SerializationFormat.Yaml;
        }
    }
}