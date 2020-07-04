namespace Firefly.CloudFormation.CloudFormation.Parsers
{
    using System;

    using Newtonsoft.Json.Linq;

    using YamlDotNet.RepresentationModel;

    /// <summary>
    /// Represents a resource stanza read from a template.
    /// A reference to the resource is included so that it can be directly modified.
    /// </summary>
    public class TemplateResource
    {
        /// <summary>
        /// Reference to the raw resource
        /// </summary>
        private readonly object rawResource;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateResource"/> class.
        /// </summary>
        /// <param name="rawResource">The raw resource.</param>
        /// <param name="format">The format.</param>
        /// <param name="logicalName">Name of the logical.</param>
        /// <param name="resourceType">Type of the resource.</param>
        internal TemplateResource(object rawResource, SerializationFormat format, string logicalName, string resourceType)
        {
            this.rawResource = rawResource;
            this.Format = format;
            this.LogicalName = logicalName;
            this.ResourceType = resourceType;
        }

        /// <summary>
        /// Gets the format of the contained resource.
        /// </summary>
        /// <value>
        /// The resource format.
        /// </value>
        public SerializationFormat Format { get; }

        /// <summary>
        /// Gets the logical resource name as read from the template.
        /// </summary>
        /// <value>
        /// Logical resource name.
        /// </value>
        public string LogicalName { get; }

        /// <summary>
        /// Gets the type of the resource, e.g. <c>AWS::EC2::Instance</c>.
        /// </summary>
        /// <value>
        /// The type of the resource.
        /// </value>
        public string ResourceType { get; }

        /// <summary>
        /// Casts the resource as a <see cref="JObject"/>. The call will fail if the input template was YAML
        /// </summary>
        /// <exception cref="FormatException">Cannot cast YAML resource to JSON</exception>
        /// <returns>JSON resource</returns>
        public JProperty AsJson()
        {
            if (this.Format == SerializationFormat.Json)
            {
                return (JProperty)this.rawResource;
            }

            throw new FormatException("Cannot cast YAML resource to JSON");
        }

        /// <summary>
        /// Casts the resource as a <see cref="YamlMappingNode"/>. The call will fail if the input template was JSON
        /// </summary>
        /// <returns>
        /// YAML object
        /// </returns>
        /// <exception cref="FormatException">Cannot cast JSON resource to YAML</exception>
        public YamlMappingNode AsYaml()
        {
            if (this.Format == SerializationFormat.Yaml)
            {
                return (YamlMappingNode)this.rawResource;
            }

            throw new FormatException("Cannot cast JSON resource to YAML");
        }
    }
}