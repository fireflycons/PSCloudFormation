namespace Firefly.PSCloudFormation.Terraform.State
{
    /// <summary>
    /// Dependency between two resources. Used in building directed edge graph.
    /// </summary>
    internal class ResourceDependency
    {
        /// <summary>
        /// Gets or sets the target attribute.
        /// </summary>
        /// <value>
        /// The target attribute.
        /// </value>
        public string TargetAttribute { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the target attribute is an array type.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the target attribute is an array type; otherwise, <c>false</c>.
        /// </value>
        public bool IsArrayMember { get; set; }
    }
}