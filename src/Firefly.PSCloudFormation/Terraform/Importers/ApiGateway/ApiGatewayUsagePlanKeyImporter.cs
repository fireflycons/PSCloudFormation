namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Imports <c>USAGE-PLAN-ID/USAGE-PLAN-KEY-ID</c>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class ApiGatewayUsagePlanKeyImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayUsagePlanKeyImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        public ApiGatewayUsagePlanKeyImporter(
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
            return string.Join("/", this.Resource.PhysicalId.Split(':').Reverse());
        }
    }
}