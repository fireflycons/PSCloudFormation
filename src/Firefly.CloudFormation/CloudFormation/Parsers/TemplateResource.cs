namespace Firefly.CloudFormation.CloudFormation.Parsers
{
    using System;
    using System.Linq;

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

        /// <summary>
        /// <para>
        /// Updates a property of a CloudFormation Resource in the loaded template.
        /// </para>
        /// <para>
        /// You would want to do this if you were implementing the functionality of <c>aws cloudformation package</c>
        /// to rewrite local file paths to S3 object locations.
        /// </para>
        /// </summary>
        /// <param name="propertyPath">Path to the property you want to set within this resource's <c>Properties</c> section,
        /// e.g. for a <c>AWS::Glue::Job</c> the path would be <c>Command/ScriptLocation</c>.
        /// </param>
        /// <param name="newValue">The new value.</param>
        /// <exception cref="FormatException">Resource format is unknown (not JSON or YAML)</exception>
        public void UpdateResourceProperty(string propertyPath, object newValue)
        {
            switch (this.Format)
            {
                case SerializationFormat.Json:

                    this.UpdateJsonResourceProperty(propertyPath, newValue);
                    break;

                case SerializationFormat.Yaml:

                    this.UpdateYamlResourceProperty(propertyPath, newValue);
                    break;

                default:

                    throw new FormatException("Resource format is unknown (not JSON or YAML)");
            }
        }

        /// <summary>
        /// <c>UpdateResourceProperty</c> implementation for JSON templates
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="newValue">The new value.</param>
        private void UpdateJsonResourceProperty(string propertyPath, object newValue)
        {
            if (propertyPath == null)
            {
                throw new ArgumentNullException(nameof(propertyPath));
            }

            var resourceProperties = this.AsJson().Children<JObject>()["Properties"].First();
            var pathSegments = propertyPath.Split('/');
            var newPropertyValue =
                (JToken)TemplateParser.SerializeObjectGraphToRepresentationModel(newValue, SerializationFormat.Json);

            var propertyToSet = resourceProperties;
            JObject parent = null;

            // Walk to requested property
            foreach (var segment in pathSegments)
            {
                // This will ultimately get the JValue associated with the property we want to change
                parent = (JObject)propertyToSet;
                propertyToSet = propertyToSet[segment];

                if (propertyToSet == null)
                {
                    throw new FormatException($"Cannot find resource property {propertyPath} in resource {this.LogicalName}");
                }
            }

            JToken newToken = new JProperty(pathSegments.Last(), newPropertyValue);

            propertyToSet.Parent.Remove();

            // ReSharper disable once PossibleNullReferenceException - Can't be null because the above foreach will iterate at least once.
            parent.Add(newToken);
        }

        /// <summary>
            /// <c>UpdateResourceProperty</c> implementation for YAML templates
            /// </summary>
            /// <param name="propertyPath">The property path.</param>
            /// <param name="newValue">The new value.</param>
            /// <exception cref="FormatException">
            /// Cannot find resource property {propertyPath} in resource {this.LogicalName}
            /// or
            /// Cannot find resource property {propertyPath} in resource {this.LogicalName}
            /// </exception>
            private void UpdateYamlResourceProperty(string propertyPath, object newValue)
        {
            var resourceProperties = this.AsYaml().Children[new YamlScalarNode("Properties")];
            var pathSegments = propertyPath.Split('/');
            var newPropertyValue =
                (YamlNode)TemplateParser.SerializeObjectGraphToRepresentationModel(newValue, SerializationFormat.Yaml);

            var propertyToSet = (YamlNode)resourceProperties;
            YamlMappingNode parentNode = null;

            // Walk to requested property
            foreach (var segment in pathSegments)
            {
                var nodeName = new YamlScalarNode(segment);

                if (propertyToSet is YamlMappingNode mappingNode)
                {
                    if (mappingNode.Children.ContainsKey(nodeName))
                    {
                        parentNode = mappingNode;
                        propertyToSet = mappingNode.Children[nodeName];
                    }
                    else
                    {
                        throw new FormatException($"Cannot find resource property {propertyPath} in resource {this.LogicalName}");
                    }
                }
            }

            if (parentNode == null)
            {
                throw new FormatException($"Cannot find resource property {propertyPath} in resource {this.LogicalName}");
            }

            if (newPropertyValue is YamlScalarNode node && propertyToSet is YamlScalarNode scalarNode)
            {
                scalarNode.Value = node.Value;
            }
            else if (newPropertyValue.NodeType != propertyToSet.NodeType)
            {
                // We need to remove and replace the node
                var nodeToReplace = new YamlScalarNode(pathSegments.Last());
                parentNode.Children.Remove(nodeToReplace);
                parentNode.Children.Add(nodeToReplace, newPropertyValue);
            }
        }
    }
}