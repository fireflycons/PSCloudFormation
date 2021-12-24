namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;
    using System.Linq;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation;
    using Firefly.CloudFormationParser;

    /// <summary>
    /// Settings object passed to the exporter mechanism
    /// </summary>
    /// <seealso cref="ITerraformExportSettings" />
    internal class TerraformExportSettings : ITerraformExportSettings
    {
        /// <inheritdoc />
        public bool AddDefaultTag { get; set; }

        /// <inheritdoc />
        public string AwsAccountId { get; set; }

        /// <inheritdoc />
        public string AwsRegion { get; set; }

        /// <inheritdoc />
        public IAmazonCloudFormation CloudFormationClient { get; set; }

        /// <inheritdoc />
        public bool ExportNestedStacks { get; set; }

        /// <inheritdoc />
        public bool IsRootModule => this.ModuleDirectory == ".";

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public string ModuleDirectory { get; set; } = ".";

        /// <inheritdoc />
        public IReadOnlyCollection<CloudFormationResource> Resources { get; set; }

        /// <inheritdoc />
        public ITerraformRunner Runner { get; set; }

        /// <inheritdoc />
        public IReadOnlyCollection<Export> StackExports { get; set; }

        /// <inheritdoc />
        public string StackName { get; set; }

        /// <inheritdoc />
        public ITemplate Template { get; set; }

        /// <inheritdoc />
        public string WorkspaceDirectory { get; set; }

        /// <inheritdoc />
        public ITerraformExportSettings CopyWith(
            ITemplate template,
            IEnumerable<CloudFormationResource> resources,
            string stackName,
            string moduleDirectory)
        {
            var newSettings = (TerraformExportSettings)this.MemberwiseClone();
            newSettings.Template = template;
            newSettings.Resources = resources.ToList();
            newSettings.ModuleDirectory = moduleDirectory;
            newSettings.StackName = stackName;

            return newSettings;
        }
    }
}