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
        public Resource Parent { get; set; }

        /// <summary>
        /// Determine whether this resource's attributes reference the ID of the given resource
        /// </summary>
        /// <param name="resource">The resource to check.</param>
        /// <returns>A <see cref="ResourceDependency"/> if there is a reference; else <c>null</c></returns>
        public ResourceDependency References(Resource resource)
        {
            var id = resource.ResourceInstance.Id;
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
                    this.Dependencies = new List<string> { resource.Address };
                }
                else
                {
                    this.Dependencies.Add(resource.Address);
                }

                break;
            }

            return dependency;
        }
    }
}