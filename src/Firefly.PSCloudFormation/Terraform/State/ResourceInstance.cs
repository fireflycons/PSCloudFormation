namespace Firefly.PSCloudFormation.Terraform.State
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents an instance of a resource in the state file.
    /// </summary>
    [DebuggerDisplay("{Id}")]
    internal class ResourceInstance
    {
        /// <summary>
        /// Gets or sets the schema version.
        /// </summary>
        /// <value>
        /// The schema version.
        /// </value>
        [JsonProperty("schema_version")]
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        [JsonProperty("attributes")]
        public JObject Attributes { get; set; }

        /// <summary>
        /// Gets or sets the sensitive attributes.
        /// </summary>
        /// <value>
        /// The sensitive attributes.
        /// </value>
        [JsonProperty("sensitive_attributes")]
        public List<string> SensitiveAttributes { get; set; }

        /// <summary>
        /// Gets or sets the provider metadata.
        /// </summary>
        /// <value>
        /// The provider metadata.
        /// </value>
        [JsonProperty("private")]
        public string ProviderMetadata { get; set; }

        /// <summary>
        /// Gets or sets the dependencies.
        /// </summary>
        /// <value>
        /// The dependencies.
        /// </value>
        [JsonProperty("dependencies", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Dependencies { get; set; }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [JsonIgnore]
        // ReSharper disable once AssignNullToNotNullAttribute - All resources in state file have an ID.
        public string Id => this.Attributes["id"].Value<string>();

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        [JsonIgnore]
        public ResourceDeclaration Parent { get; set; }

        /// <summary>
        /// Determine whether this resource's attributes reference the ID of the given resource
        /// </summary>
        /// <param name="resourceDeclaration">The resource to check.</param>
        /// <returns>A <see cref="ResourceDependency"/> if there is a reference; else <c>null</c></returns>
        public ResourceDependency References(ResourceDeclaration resourceDeclaration)
        {
            var id = resourceDeclaration.ResourceInstance.Id;
            ResourceDependency dependency = null;

            // Walk JSON attributes looking for a match on ID
            foreach (var attr in this.Attributes)
            {
                var key = attr.Key;
                var value = attr.Value;

                if (key == "id")
                {
                    continue;
                }

                switch (value)
                {
                    case JValue jv:

                        if (id == jv.Value<string>())
                        {
                            dependency = new ResourceDependency { TargetAttribute = key, IsArrayMember = false };
                        }

                        break;

                    case JArray ja:

                        foreach (var arrayValue in ja.Where(j => j.Type == JTokenType.String))
                        {
                            if (id == ((JValue)arrayValue).Value<string>())
                            {
                                dependency = new ResourceDependency { TargetAttribute = key, IsArrayMember = true };
                            }
                        }

                        break;
                }

                if (dependency == null)
                {
                    continue;
                }

                // Add input resource to this resource's dependencies
                if (this.Dependencies == null)
                {
                    this.Dependencies = new List<string> { resourceDeclaration.Address };
                }
                else
                {
                    this.Dependencies.Add(resourceDeclaration.Address);
                }

                break;
            }

            return dependency;
        }

        public List<JToken> FindId(string id)
        {
            var foundValues = new List<JToken>();
            this.WalkNode(this.Attributes, id, foundValues);
            return foundValues;
        }


        /// <summary>
        /// Recursively walk the properties of a <c>JToken</c>
        /// </summary>
        /// <param name="node">The starting node.</param>
        private void WalkNode(
            JToken node, string id, List <JToken> results)
        {
            switch (node.Type)
            {
                case JTokenType.Object:

                    foreach (var child in node.Children<JProperty>())
                    {
                        if (child.Value is JValue value && value.Value is string s && s.Contains(id))
                        {
                            results.Add(child);
                        }

                        this.WalkNode(child.Value, id, results);
                    }

                    // do something?
                    break;

                case JTokenType.Array:

                    foreach (var child in node.Children())
                    {
                        if (child is JValue value && value.Value is string s && s.Contains(id))
                        {
                            results.Add(node);
                        }

                        this.WalkNode(child, id, results);
                    }

                    break;

                default:

                    // Value
                    break;
            }
        }
    }
}