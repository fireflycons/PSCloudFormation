namespace Firefly.PSCloudFormation.Terraform.State
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.Hcl;

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

        public HashSet<JToken> FindId(IReferencedItem id)
        {
            var foundValues = new HashSet<JToken>();
            this.WalkNode(this.Attributes, id, foundValues);
            return foundValues;
        }
        
        /// <summary>
        /// Recursively walk the properties of a <c>JToken</c>
        /// </summary>
        /// <param name="node">The starting node.</param>
        /// <param name="id">The id to find</param>
        /// <param name="results">List into which matches are placed</param>
        private void WalkNode(
            JToken node, IReferencedItem id, ICollection<JToken> results)
        {
            switch (node.Type)
            {
                case JTokenType.Object:

                    foreach (var child in node.Children<JProperty>())
                    {
                        this.CheckReference(child, child.Value, id, results);
                    }

                    break;

                case JTokenType.Array:

                    foreach (var child in node.Children())
                    {
                        this.CheckReference(node, child, id, results);
                    }

                    break;
            }
        }

        private void CheckReference(JToken node, JToken child, IReferencedItem id, ICollection<JToken> results)
        {
            if (child is JValue value && value.Value is string stringValue)
            {
                if (id.IsScalar)
                {
                    if (stringValue.Contains(id.ScalarIdentity))
                    {
                        results.Add(node);
                    }
                }
                else
                {
                    // If any of the id's values match, then it is a match
                    if (id.ListIdentity.Any(item => stringValue.Contains(item)))
                    {
                        results.Add(node);
                    }
                }
            }
            else
            {
                this.WalkNode(child, id, results);
            }
        }
    }
}