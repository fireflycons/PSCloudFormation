namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormationParser;

    /// <summary>
    /// Settings object passed to the exporter mechanism
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.ITerraformSettings" />
    internal class TerraformSettings : ITerraformSettings
    {
        /// <inheritdoc />
        public string AwsAccountId { get; set; }

        /// <inheritdoc />
        public string AwsRegion { get; set; }

        /// <inheritdoc />
        public IReadOnlyCollection<StackResource> PhysicalResources { get; set; }

        /// <inheritdoc />
        public IReadOnlyCollection<CloudFormationResource> Resources { get; set;  }

        /// <inheritdoc />
        public ITerraformRunner Runner { get; set; }

        /// <inheritdoc />
        public string StackName { get; set; }

        /// <inheritdoc />
        public ITemplate Template { get; set; }

        /// <inheritdoc />
        public string WorkspaceDirectory { get; set; }

        /// <inheritdoc />
        public IReadOnlyCollection<Export> StackExports { get; set;  }
    }
}