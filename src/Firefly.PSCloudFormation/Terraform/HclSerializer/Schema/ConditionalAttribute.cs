namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
{
    /// <summary>
    /// An attribute that should not be emitted when it has a specific value
    /// </summary>
    internal class ConditionalAttribute
    {
        /// <summary>
        /// Gets or sets the attribute name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// If the attribute has this value, then do not emit it
        /// </value>
        public string Value { get; set; }
    }
}