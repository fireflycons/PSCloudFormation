namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Handles import of gateway-dependent resources having import format of REST-API-ID/PHYSICAL-ID
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ApiGateway.AbstractApiGatewayRestApiImporter" />
    internal class ApiGatewayApiDependencyImporter : AbstractApiGatewayRestApiImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayApiDependencyImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        public ApiGatewayApiDependencyImporter(
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
            var restApi = this.GetRestApiId();

            return restApi == null ? null : $"{restApi}/{this.Resource.PhysicalId}";
        }
    }
}