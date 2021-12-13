namespace Firefly.PSCloudFormation.Terraform.State
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents an instance of a resource in the state file.
    /// </summary>
    [DebuggerDisplay("{Id}")]
    internal class StateFileResourceInstance
    {
        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        [JsonProperty("attributes")]
        public JObject Attributes { get; set; }

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
        public StateFileResourceDeclaration Parent { get; set; }

        /// <summary>
        /// Gets or sets the provider metadata.
        /// </summary>
        /// <value>
        /// The provider metadata.
        /// </value>
        [JsonProperty("private")]
        public string ProviderMetadata { get; set; }

        /// <summary>
        /// Gets or sets the schema version.
        /// </summary>
        /// <value>
        /// The schema version.
        /// </value>
        [JsonProperty("schema_version")]
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the sensitive attributes.
        /// </summary>
        /// <value>
        /// The sensitive attributes.
        /// </value>
        [JsonProperty("sensitive_attributes")]
        public List<string> SensitiveAttributes { get; set; }

        /// <summary>
        /// Finds tokens in the state file whose values match the given referenced item
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="allowInterpolation">if set to <c>true</c>, then include partial matches - i.e. a string interpolation will be emitted.</param>
        /// <returns>Set of matching attributes.</returns>
        public HashSet<JToken> FindId(IReferencedItem id, bool allowInterpolation)
        {
            var context = new FindIdContext(id, allowInterpolation);
            this.Attributes.Accept(new FindIdVisitor(), context);
            return context.FoundValues;
        }

        /// <summary>
        /// Context class for <see cref="FindId"/>
        /// </summary>
        private class FindIdContext : IJsonVisitorContext<FindIdContext>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FindIdContext"/> class.
            /// </summary>
            /// <param name="referencedItem">The referenced item.</param>
            /// <param name="allowInterpolation">if set to <c>true</c> [allow interpolation].</param>
            public FindIdContext(IReferencedItem referencedItem, bool allowInterpolation)
            {
                this.ReferencedItem = referencedItem;
                this.AllowInterpolation = allowInterpolation;
            }

            /// <summary>
            /// Gets a value indicating whether [allow interpolation].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [allow interpolation]; otherwise, <c>false</c>.
            /// </value>
            public bool AllowInterpolation { get; }

            /// <summary>
            /// Gets the referenced item.
            /// </summary>
            /// <value>
            /// The referenced item.
            /// </value>
            public IReferencedItem ReferencedItem { get; }

            /// <summary>
            /// Gets the found values.
            /// </summary>
            /// <value>
            /// The found values.
            /// </value>
            public HashSet<JToken> FoundValues { get; } = new HashSet<JToken>();

            /// <inheritdoc />
            public FindIdContext Next(int index)
            {
                return this;
            }

            /// <inheritdoc />
            public FindIdContext Next(string name)
            {
                return this;
            }
        }

        /// <summary>
        /// Visitor implementation for <see cref="FindId"/>
        /// </summary>
        private class FindIdVisitor : JValueVisitor<FindIdContext>
        {
            /// <inheritdoc />
            protected override void VisitString(JValue json, FindIdContext context)
            {
                var stringValue = json.Value<string>();

                if (context.ReferencedItem.IsScalar)
                {
                    if ((context.AllowInterpolation && stringValue.Contains(context.ReferencedItem.ScalarIdentity))
                        || stringValue == context.ReferencedItem.ScalarIdentity)
                    {
                        context.FoundValues.Add(json.Parent);
                    }
                }
                else
                {
                    // If any of the id's values match, then it is a match
                    if (context.ReferencedItem.ListIdentity.Any(
                        item => (context.AllowInterpolation && stringValue.Contains(item)) || stringValue == item))
                    {
                        context.FoundValues.Add(json.Parent);
                    }
                }
            }
        }
    }
}