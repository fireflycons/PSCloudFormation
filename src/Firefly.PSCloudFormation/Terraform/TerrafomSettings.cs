namespace Firefly.PSCloudFormation.Terraform
{
    /// <summary>
    /// Settings object passed to the exporter mechanism
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.ITerraformSettings" />
    internal class TerrafomSettings : ITerraformSettings
    {
        /// <summary>
        /// Gets or sets the AWS region.
        /// </summary>
        /// <value>
        /// The AWS region.
        /// </value>
        public string AwsRegion { get; set; }

        /// <summary>
        /// Gets or sets the runner that drives the Terraform binary.
        /// </summary>
        /// <value>
        /// The runner.
        /// </value>
        public ITerraformRunner Runner { get; set; }

        /// <summary>
        /// Gets or sets the Terraform workspace directory where the generated code and state will be stored.
        /// </summary>
        /// <value>
        /// The workspace directory.
        /// </value>
        public string WorkspaceDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [non interactive].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [non interactive]; otherwise, <c>false</c>.
        /// </value>
        public bool NonInteractive { get; set; }
    }
}