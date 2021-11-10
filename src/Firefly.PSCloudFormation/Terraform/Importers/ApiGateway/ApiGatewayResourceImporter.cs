namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Imports REST-API-ID/RESOURCE-ID
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ApiGateway.AbstractApiGatewayRestApiImporter" />
    internal class ApiGatewayResourceImporter : AbstractApiGatewayRestApiImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayResourceImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        public ApiGatewayResourceImporter(
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
            return this.GetResourceId();
        }

        /// <summary>
        /// Gets the resource identifier which is REST-API-ID/RESOURCE-ID.
        /// </summary>
        /// <returns>The resource identifier.</returns>
        protected string GetResourceId()
        {
            var restApi = this.GetRestApiId();

            if (restApi == null)
            {
                return null;
            }

            return $"{restApi}/{this.Resource.PhysicalId}";
        }
    }
}