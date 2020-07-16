namespace Firefly.CloudFormation.Parsers
{
    using System;
    using System.Linq;

    using Firefly.CloudFormation.Model;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Concrete <see cref="TemplateResource"/> for JSON templates
    /// </summary>
    /// <seealso cref="TemplateResource" />
    public class JsonTemplateResource : TemplateResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTemplateResource"/> class.
        /// </summary>
        /// <param name="rawResource">The raw resource.</param>
        /// <param name="logicalName">Name of the logical.</param>
        /// <param name="resourceType">Type of the resource.</param>
        internal JsonTemplateResource(object rawResource, string logicalName, string resourceType)
            : base(rawResource, SerializationFormat.Json, logicalName, resourceType)
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
            return this.GetJsonResourcePropertyValue(propertyPath);
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
            this.UpdateJsonResourceProperty(propertyPath, newValue);
        }

        private JToken GetJsonProperty(string propertyPath, out JObject parent)
        {
            var pathSegments = propertyPath.Split('.');
            var resourceProperties = this.AsJson().Children<JObject>()["Properties"].First();
            var property = resourceProperties;
            parent = null;

            // Walk to requested property
            foreach (var segment in pathSegments)
            {
                // This will ultimately get the JValue associated with the property we want to change
                parent = (JObject)property;
                property = property[segment];

                if (property == null)
                {
                    throw new FormatException(
                        $"Cannot find resource property {propertyPath} in resource {this.LogicalName}");
                }
            }

            return property;
        }

        private string GetJsonResourcePropertyValue(string propertyPath)
        {
            if (propertyPath == null)
            {
                throw new ArgumentNullException(nameof(propertyPath));
            }

            // ReSharper disable once UnusedVariable
            var propertyToGet = this.GetJsonProperty(propertyPath, out var parent) as JValue;

            return propertyToGet?.Value<string>();
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

            var newPropertyValue =
                (JToken)TemplateParser.SerializeObjectGraphToRepresentationModel(newValue, SerializationFormat.Json);

            var propertyToSet = this.GetJsonProperty(propertyPath, out var parent);

            JToken newToken = new JProperty(propertyPath.Split('.').Last(), newPropertyValue);

            propertyToSet.Parent.Remove();

            // ReSharper disable once PossibleNullReferenceException - Can't be null because the above foreach will iterate at least once.
            parent.Add(newToken);
        }
    }
}