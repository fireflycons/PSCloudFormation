namespace Firefly.CloudFormation.Parsers
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Parser for YAML parameter files
    /// </summary>
    /// <seealso cref="ParameterFileParser" />
    public class YamlParameterFileParser : ParameterFileParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="YamlParameterFileParser"/> class.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        public YamlParameterFileParser(string fileContent)
            : base(fileContent)
        {
        }

        /// <summary>
        /// <para>
        /// Parses a parameter file.
        /// </para>
        /// <para>
        /// This is a JSON or YAML list of parameter structures with fields <c>ParameterKey</c> and <c>ParameterValue</c>.
        /// This is similar to <c>aws cloudformation create-stack</c>  except the other fields defined for that are ignored here.
        /// Parameters not supplied to an update operation are assumed to be <c>UsePreviousValue</c>.
        /// </para>
        /// </summary>
        /// <returns>
        /// A dictionary of parameter key-value pairs
        /// </returns>
        public override IDictionary<string, string> ParseParameterFile()
        {
            if (string.IsNullOrEmpty(this.FileContent))
            {
                return new Dictionary<string, string>();
            }

            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var parameters = deserializer.Deserialize<List<Param>>(this.FileContent);

            return parameters.ToDictionary(p => p.ParameterKey, p => p.ParameterValue);
        }

        /// <summary>
        /// Representation of parameter object in parameter file being parsed
        /// </summary>
        // ReSharper disable once ClassNeverInstantiated.Local
        private class Param
        {
            /// <summary>
            /// Gets or sets the parameter key.
            /// </summary>
            /// <value>
            /// The parameter key.
            /// </value>
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string ParameterKey { get; set; }

            /// <summary>
            /// Gets or sets the parameter value.
            /// </summary>
            /// <value>
            /// The parameter value.
            /// </value>
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string ParameterValue { get; set; }
        }
    }
}