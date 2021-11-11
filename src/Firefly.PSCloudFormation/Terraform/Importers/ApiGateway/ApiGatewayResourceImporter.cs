namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/api_gateway_resource#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ApiGateway.AbstractApiGatewayRestApiImporter" />
    internal class ApiGatewayResourceImporter : AbstractApiGatewayRestApiImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayResourceImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public ApiGatewayResourceImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
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

            return $"{restApi}/{this.ImportSettings.Resource.PhysicalId}";
        }
    }
}