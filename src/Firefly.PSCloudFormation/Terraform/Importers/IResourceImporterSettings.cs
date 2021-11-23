namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System.Collections.Generic;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Settings data passed to resource importers
    /// </summary>
    internal interface IResourceImporterSettings
    {
        /// <summary>
        /// Gets the error list.
        /// </summary>
        /// <value>
        /// The errors.
        /// </value>
        IList<string> Errors { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        ILogger Logger { get; }

        /// <summary>
        /// Gets the resource being imported.
        /// </summary>
        /// <value>
        /// The resource.
        /// </value>
        ResourceImport Resource { get; }

        /// <summary>
        /// Gets the list of resources to import.
        /// </summary>
        /// <value>
        /// The resources to import.
        /// </value>
        IReadOnlyCollection<ResourceImport> ResourcesToImport { get; }

        /// <summary>
        /// Gets the warning list.
        /// </summary>
        /// <value>
        /// The warnings.
        /// </value>
        IList<string> Warnings { get; }
    }
}