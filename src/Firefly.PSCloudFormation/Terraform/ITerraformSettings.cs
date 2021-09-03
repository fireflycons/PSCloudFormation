namespace Firefly.PSCloudFormation.Terraform
{
    /// <summary>
    /// Settings for terraform exporter
    /// </summary>
    internal interface ITerraformSettings
    {
        /// <summary>
        /// Gets the AWS region.
        /// </summary>
        /// <value>
        /// The AWS region.
        /// </value>
        string AwsRegion { get; }

        /// <summary>
        /// Gets the Terraform workspace directory where the generated code and state will be stored.
        /// </summary>
        /// <value>
        /// The workspace directory.
        /// </value>
        string WorkspaceDirectory { get; }

        /// <summary>
        /// Gets the runner that drives the Terraform binary.
        /// </summary>
        /// <value>
        /// The runner.
        /// </value>
        ITerraformRunner Runner { get; }

        /// <summary>
        /// Gets a value indicating whether [non interactive].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [non interactive]; otherwise, <c>false</c>.
        /// </value>
        bool NonInteractive { get; }
    }
}