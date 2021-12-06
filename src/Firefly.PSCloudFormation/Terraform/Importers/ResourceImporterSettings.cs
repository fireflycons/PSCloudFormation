namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System.Collections.Generic;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Settings data passed to resource importers
    /// </summary>
    internal class ResourceImporterSettings : IResourceImporterSettings
    {
        /// <inheritdoc />
        public IList<string> Errors { get; set; }

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public ResourceMapping Resource { get; set; }

        /// <inheritdoc />
        public IReadOnlyCollection<ResourceMapping> ResourcesToImport { get; set; }

        /// <inheritdoc />
        public IList<string> Warnings { get; set; }
    }
}