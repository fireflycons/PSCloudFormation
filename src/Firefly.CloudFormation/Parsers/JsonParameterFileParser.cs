namespace Firefly.CloudFormation.Parsers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Parser for JSON parameter files
    /// </summary>
    /// <seealso cref="ParameterFileParser" />
    public class JsonParameterFileParser : ParameterFileParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonParameterFileParser"/> class.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        public JsonParameterFileParser(string fileContent)
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

            using (var reader = new StringReader(this.FileContent))
            {
                return ((JArray)JToken.ReadFrom(new JsonTextReader(reader))).Cast<JObject>().ToDictionary(
                    o => o["ParameterKey"].ToString(),
                    o => o["ParameterValue"].ToString());
            }
        }
    }
}