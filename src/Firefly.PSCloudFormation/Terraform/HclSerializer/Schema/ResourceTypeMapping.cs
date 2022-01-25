namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
{
    using System.Diagnostics;

    using Newtonsoft.Json;

    /// <summary>
    /// Maps an AWS resource type to equivalent Terraform resource.
    /// This is backed by the embedded resource <c>terraform-resource-map.json</c>
    /// </summary>
    [DebuggerDisplay("{Aws} -> {Terraform}")]
    internal class ResourceTypeMapping
    {
        /// <summary>
        /// Gets or sets the AWS type name.
        /// </summary>
        /// <value>
        /// The AWS type name.
        /// </value>
        [JsonProperty("AWS")]
        public string Aws { get; set; }

        /// <summary>
        /// Gets or sets the terraform type name.
        /// </summary>
        /// <value>
        /// The terraform.
        /// </value>
        [JsonProperty("TF")]
        public string Terraform { get; set; }
    }
}