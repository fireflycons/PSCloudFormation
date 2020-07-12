namespace Firefly.CloudFormation.Parsers
{
    using System.Collections.Generic;
    using System.IO;

    using Firefly.CloudFormation.Model;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using YamlDotNet.RepresentationModel;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Base class for CLoudFormation template parsers.
    /// </summary>
    public abstract class TemplateParser : InputFileParser
    {
        /// <summary>
        /// The description key name
        /// </summary>
        protected const string DescriptionKeyName = "Description";

        /// <summary>
        /// Amount of padding to add to resource names to include random chars added by CloudFormation
        /// </summary>
        protected const int NestedStackPadWidth = 14;

        /// <summary>
        /// The nested stack type
        /// </summary>
        protected const string NestedStackType = "AWS::CloudFormation::Stack";

        /// <summary>
        /// The parameter key name
        /// </summary>
        protected const string ParameterKeyName = "Parameters";

        /// <summary>
        /// The resource key name
        /// </summary>
        protected const string ResourceKeyName = "Resources";

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateParser"/> class.
        /// </summary>
        /// <param name="templateBody">The template body.</param>
        protected TemplateParser(string templateBody)
            : base(templateBody)
        {
        }

        /// <summary>
        /// Creates a Cloud Formation template parser.
        /// </summary>
        /// <param name="templateBody">The template body.</param>
        /// <returns>A new <see cref="TemplateParser"/></returns>
        /// <exception cref="InvalidDataException">Template body is empty</exception>
        public static TemplateParser Create(string templateBody)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (GetInputFileFormat(templateBody))
            {
                case SerializationFormat.Json:

                    return new JsonTemplateParser(templateBody);

                case SerializationFormat.Yaml:

                    return new YamlTemplateParser(templateBody);

                default:

                    throw new InvalidDataException("Template body is empty");
            }
        }

        /// <summary>
        /// Serializes an object graph to JSON or YAML string
        /// </summary>
        /// <param name="objectGraph">The object graph.</param>
        /// <param name="format">The required serialization format.</param>
        /// <returns>Object graph serialized to string in requested format.</returns>
        public static string SerializeObjectGraphToString(object objectGraph, SerializationFormat format)
        {
            switch (format)
            {
                case SerializationFormat.Json:

                    return JsonConvert.SerializeObject(objectGraph, Formatting.Indented);

                case SerializationFormat.Yaml:

                    return new SerializerBuilder().Build().Serialize(objectGraph);

                default:

                    throw new System.InvalidOperationException($"Unsupported format: {format}");
            }
        }

        /// <summary>
        /// Serializes the object graph to representation model.
        /// </summary>
        /// <param name="objectGraph">The object graph.</param>
        /// <param name="format">The format.</param>
        /// <returns>Either a <see cref="YamlNode"/> or a <see cref="JObject"/> depending on requested format.</returns>
        /// <exception cref="System.InvalidOperationException">Unsupported format: {format}</exception>
        public static object SerializeObjectGraphToRepresentationModel(object objectGraph, SerializationFormat format)
        {
            switch (format)
            {
                case SerializationFormat.Json:

                    if (objectGraph == null || objectGraph is string)
                    {
                        return new JValue(objectGraph);
                    }

                    return JObject.Parse(JsonConvert.SerializeObject(objectGraph, Formatting.Indented));

                case SerializationFormat.Yaml:

                    var yaml = new YamlStream();

                    using (var sr = new StringReader(new SerializerBuilder().Build().Serialize(objectGraph)))
                    {
                        yaml.Load(sr);
                    }

                    return yaml.Documents[0].RootNode;

                default:

                    throw new System.InvalidOperationException($"Unsupported format: {format}");
            }
        }

        /// <summary>
        /// Gets the logical resource names.
        /// </summary>
        /// <param name="stackName">Name of the parent stack. Used to prefix nested stack resources</param>
        /// <returns>List of resource names.</returns>
        public abstract IEnumerable<string> GetLogicalResourceNames(string stackName);

        /// <summary>
        /// Gets logical resource names of nested stacks declared in the given template
        /// Does not recurse these.
        /// </summary>
        /// <returns>List of nested stack logical resource names, if any.</returns>
        public IEnumerable<string> GetNestedStackNames()
        {
            return this.GetNestedStackNames(string.Empty);
        }

        /// <summary>
        /// Gets logical resource names of nested stacks declared in the given template, accounting for how CloudFormation will name them when the template runs.
        /// Does not recurse these.
        /// </summary>
        /// <param name="baseStackName">Name of the base stack</param>
        /// <returns>List of nested stack logical resource names, if any.</returns>
        public abstract IEnumerable<string> GetNestedStackNames(string baseStackName);

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <returns>List of <see cref="TemplateFileParameter"/></returns>
        public abstract IEnumerable<TemplateFileParameter> GetParameters();

        /// <summary>
        /// Gets the template resources.
        /// </summary>
        /// <returns>Enumerable of resources found in template</returns>
        public abstract IEnumerable<TemplateResource> GetResources();

        /// <summary>
        /// Gets the template description.
        /// </summary>
        /// <returns>Content of description property from template</returns>
        public abstract string GetTemplateDescription();

        /// <summary>
        /// Saves the template to the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public abstract void Save(string path);

        /// <summary>
        /// Gets the template by re-serializing the current state of the representation model.
        /// </summary>
        /// <returns>Template body as string</returns>
        public abstract string GetTemplate();
    }
}