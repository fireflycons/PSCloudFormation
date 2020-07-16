namespace Firefly.CloudFormation.Parsers
{
    using System;
    using System.IO;

    using Firefly.CloudFormation.Model;

    using Newtonsoft.Json.Linq;

    using YamlDotNet.RepresentationModel;

    /// <summary>
    /// Represents a resource stanza read from a template.
    /// A reference to the resource is included so that it can be directly modified.
    /// </summary>
    public abstract class TemplateResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateResource"/> class.
        /// </summary>
        /// <param name="rawResource">The raw resource.</param>
        /// <param name="format">The format.</param>
        /// <param name="logicalName">Name of the logical.</param>
        /// <param name="resourceType">Type of the resource.</param>
        protected TemplateResource(
            object rawResource,
            SerializationFormat format,
            string logicalName,
            string resourceType)
        {
            this.RawResource = rawResource;
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
        /// Gets the raw resource.
        /// </summary>
        /// <value>
        /// The raw resource.
        /// </value>
        protected object RawResource { get; }

        /// <summary>
        /// Casts the resource as a <see cref="JObject"/>. The call will fail if the input template was YAML
        /// </summary>
        /// <exception cref="FormatException">Cannot cast YAML resource to JSON</exception>
        /// <returns>JSON resource</returns>
        public JProperty AsJson()
        {
            if (this.Format == SerializationFormat.Json)
            {
                return (JProperty)this.RawResource;
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
                return (YamlMappingNode)this.RawResource;
            }

            throw new FormatException("Cannot cast JSON resource to YAML");
        }

        /// <summary>
        /// Gets a resource property value. Currently only leaf nodes (string values) are supported.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>The value of the property; else <c>null</c> if the property path did not resolve to a leaf node.</returns>
        public abstract string GetResourcePropertyValue(string propertyPath);

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
        public abstract void UpdateResourceProperty(string propertyPath, object newValue);

        /// <summary>
        /// Creates a representation of the template resource that can be used to modify that resource.
        /// </summary>
        /// <param name="rawResource">The raw resource.</param>
        /// <param name="logicalName">Name of the logical.</param>
        /// <param name="resourceType">Type of the resource.</param>
        /// <returns>Subclass of <see cref="TemplateResource"/> according to the type of <paramref name="rawResource"/></returns>
        /// <exception cref="InvalidDataException">Cannot parse CloudFormation resource from object of type {rawResource.GetType().FullName}</exception>
        internal static TemplateResource Create(object rawResource, string logicalName, string resourceType)
        {
            switch (rawResource)
            {
                case JToken _:

                    return new JsonTemplateResource(rawResource, logicalName, resourceType);

                case YamlNode _:

                    return new YamlTemplateResource(rawResource, logicalName, resourceType);

                default:

                    throw new InvalidDataException(
                        $"Cannot parse CloudFormation resource from object of type {rawResource.GetType().FullName}");
            }
        }
    }
}