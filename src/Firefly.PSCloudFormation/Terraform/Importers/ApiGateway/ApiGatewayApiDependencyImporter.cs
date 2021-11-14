namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    /// <summary>
    /// Handles import of gateway-dependent resources having import format of REST-API-ID/PHYSICAL-ID
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ApiGateway.AbstractApiGatewayRestApiImporter" />
    internal class ApiGatewayApiDependencyImporter : AbstractApiGatewayRestApiImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayApiDependencyImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public ApiGatewayApiDependencyImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencingPropertyPath => "RestApiId";

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            var restApi = this.GetRestApiId();

            return restApi == null ? null : $"{restApi}/{this.ImportSettings.Resource.PhysicalId}";
        }
    }
}