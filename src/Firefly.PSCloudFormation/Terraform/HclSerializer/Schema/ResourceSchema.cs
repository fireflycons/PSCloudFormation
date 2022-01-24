namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
{
    /// <summary>
    /// Everything known about a resource type.
    /// </summary>
    internal class ResourceSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSchema"/> class.
        /// </summary>
        /// <param name="resourceType">The resource type.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="traits">The traits.</param>
        public ResourceSchema(string resourceType, ProviderResourceSchema schema, IResourceTraits traits)
        {
            this.Schema = schema;
            this.ResourceType = resourceType;
            this.Traits = traits;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string ResourceType { get; }

        /// <summary>
        /// Gets the schema as per the Terraform AWS provider.
        /// </summary>
        /// <value>
        /// The schema.
        /// </value>
        public ProviderResourceSchema Schema { get; }

        /// <summary>
        /// Gets the locally maintained traits.
        /// </summary>
        /// <value>
        /// The traits.
        /// </value>
        public IResourceTraits Traits { get; }

        /// <summary>
        /// Gets an attribute schema given path to it.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Terraform AWS provider schema.</returns>
        public ValueSchema GetAttributeByPath(string path)
        {
            return this.Schema.GetAttributeByPath(path, this.Traits);
        }
    }
}