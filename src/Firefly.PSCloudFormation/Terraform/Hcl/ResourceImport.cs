namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Diagnostics;

    /// <summary>
    /// Represents a mapping between an imported AWS resource and a Terraform resource
    /// </summary>
    [DebuggerDisplay("{Address}: {PhysicalId}")]
    internal class ResourceImport
    {
        /// <summary>
        /// Gets or sets the Terraform resource address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public string Address => $"{this.TerraformType}.{this.LogicalId}";

        /// <summary>
        /// Gets or sets the AWS address (logical ID).
        /// </summary>
        /// <value>
        /// The AWS address.
        /// </value>
        public string AwsAddress => $"{this.LogicalId} ({this.AwsType})";

        /// <summary>
        /// Gets or sets the physical identifier.
        /// </summary>
        /// <value>
        /// The physical identifier.
        /// </value>
        public string PhysicalId { get; set; }

        /// <summary>
        /// Gets or sets the logical identifier.
        /// </summary>
        /// <value>
        /// The logical identifier.
        /// </value>
        public string LogicalId { get; set; }

        /// <summary>
        /// Gets or sets the AWS resource type.
        /// </summary>
        /// <value>
        /// The resource type.
        /// </value>
        public string AwsType { get; set; }

        /// <summary>
        /// Gets or sets the type of the terraform.
        /// </summary>
        /// <value>
        /// The type of the terraform.
        /// </value>
        public string TerraformType { get; set; }
    }
}