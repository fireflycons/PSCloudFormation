namespace Firefly.PSCloudFormation.Terraform
{
    /// <summary>
    /// Settings for terraform exporter
    /// </summary>
    internal interface ITerraformSettings
    {
        /// <summary>
        /// Gets the aws region.
        /// </summary>
        /// <value>
        /// The aws region.
        /// </value>
        string AwsRegion { get; }

        /// <summary>
        /// Gets the workspace directory.
        /// </summary>
        /// <value>
        /// The workspace directory.
        /// </value>
        string WorkspaceDirectory { get; }

        /// <summary>
        /// Gets the runner.
        /// </summary>
        /// <value>
        /// The runner.
        /// </value>
        ITerraformRunner Runner { get; }
    }
}