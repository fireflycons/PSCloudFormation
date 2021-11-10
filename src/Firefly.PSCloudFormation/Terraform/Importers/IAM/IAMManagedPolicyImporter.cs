namespace Firefly.PSCloudFormation.Terraform.Importers.IAM
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    internal class IAMManagedPolicyImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IAMManagedPolicyImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        public IAMManagedPolicyImporter(
            ResourceImport resource,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport,
            ITerraformSettings settings)
            : base(resource, ui, resourcesToImport, settings)
        {
        }

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            return $"arn:aws:iam::{this.Settings.AwsAccountId}:policy/{this.Resource.PhysicalId}";
        }
    }
}