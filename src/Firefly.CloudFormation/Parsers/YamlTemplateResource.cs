namespace Firefly.CloudFormation.Parsers
{
    using System;
    using System.Linq;

    using Firefly.CloudFormation.Model;

    using YamlDotNet.RepresentationModel;

    /// <summary>
    /// Concrete <see cref="TemplateResource"/> for YAML templates
    /// </summary>
    /// <seealso cref="TemplateResource" />
    public class YamlTemplateResource : TemplateResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="YamlTemplateResource"/> class.
        /// </summary>
        /// <param name="rawResource">The raw resource.</param>
        /// <param name="logicalName">Name of the logical.</param>
        /// <param name="resourceType">Type of the resource.</param>
        internal YamlTemplateResource(object rawResource, string logicalName, string resourceType)
            : base(rawResource, SerializationFormat.Yaml, logicalName, resourceType)
        {
        }

        /// <summary>
        /// Gets a resource property value. Currently only leaf nodes (string values) are supported.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>
        /// The value of the property; else <c>null</c> if the property path did not resolve to a leaf node.
        /// </returns>
        public override string GetResourcePropertyValue(string propertyPath)
        {
            return this.GetYamlResourcePropertyValue(propertyPath);
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
        /// e.g. for a <c>AWS::Glue::Job</c> the path would be <c>Command/ScriptLocation</c>.</param>
        /// <param name="newValue">The new value.</param>
        public override void UpdateResourceProperty(string propertyPath, object newValue)
        {
            this.UpdateYamlResourceProperty(propertyPath, newValue);
        }

        private YamlNode GetYamlProperty(string propertyPath, out YamlMappingNode parentNode)
        {
            var pathSegments = propertyPath.Split('.');
            var resourceProperties = this.AsYaml().Children[new YamlScalarNode("Properties")];
            var property = resourceProperties;
            parentNode = null;

            // Walk to requested property
            foreach (var nodeName in pathSegments.Select(segment => new YamlScalarNode(segment)))
            {
                if (property is YamlMappingNode mappingNode)
                {
                    if (mappingNode.Children.ContainsKey(nodeName))
                    {
                        parentNode = mappingNode;
                        property = mappingNode.Children[nodeName];
                    }
                    else
                    {
                        throw new FormatException(
                            $"Cannot find resource property {propertyPath} in resource {this.LogicalName}");
                    }
                }
            }

            if (parentNode == null)
            {
                throw new FormatException(
                    $"Cannot find resource property {propertyPath} in resource {this.LogicalName}");
            }

            return property;
        }

        private string GetYamlResourcePropertyValue(string propertyPath)
        {
            if (propertyPath == null)
            {
                throw new ArgumentNullException(nameof(propertyPath));
            }

            // ReSharper disable once UnusedVariable
            var propertyToGet = this.GetYamlProperty(propertyPath, out var parent) as YamlScalarNode;

            return propertyToGet?.ToString();
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
            var newPropertyValue =
                (YamlNode)TemplateParser.SerializeObjectGraphToRepresentationModel(newValue, SerializationFormat.Yaml);

            var propertyToSet = this.GetYamlProperty(propertyPath, out var parentNode);

            if (newPropertyValue is YamlScalarNode node && propertyToSet is YamlScalarNode scalarNode)
            {
                scalarNode.Value = node.Value;
            }
            else if (newPropertyValue.NodeType != propertyToSet.NodeType)
            {
                // We need to remove and replace the node
                var nodeToReplace = new YamlScalarNode(propertyPath.Split('.').Last());
                parentNode.Children.Remove(nodeToReplace);
                parentNode.Children.Add(nodeToReplace, newPropertyValue);
            }
        }
    }
}