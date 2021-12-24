namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Diagnostics;

    /// <summary>
    /// Represents a summary mapping between an imported AWS resource and a Terraform resource.
    /// Maps the resource types, logical name and physical ID.
    /// </summary>
    [DebuggerDisplay("{ImportAddress}: {PhysicalId}")]
    internal class ResourceMapping
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
        /// Gets the module-specific import address for this resource.
        /// </summary>
        /// <value>
        /// The import address.
        /// </value>
        public string ImportAddress =>
            string.IsNullOrEmpty(this.Module) ? this.Address : $"module.{this.Module}.{this.Address}";

        /// <summary>
        /// Gets or sets the physical identifier, as in the identifier given to the resource by a CloudFormation deployment
        /// which is also the 'id' attribute of the resource within terraform state.
        /// </summary>
        /// <value>
        /// The physical identifier.
        /// </value>
        public string PhysicalId { get; set; }

        /// <summary>
        /// Gets or sets the logical identifier, as in the name of the resource in either CloudFormation or HCL.
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

        /// <summary>
        /// Gets or sets the module this resource belongs to.
        /// </summary>
        /// <value>
        /// The module.
        /// </value>
         public string Module { get; set; }
    }
}