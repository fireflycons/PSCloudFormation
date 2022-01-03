namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGatewayV2
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/apigatewayv2_stage#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class ApiGatewayV2StageImporter : AbstractApiGatewayV2ApiImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayV2StageImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public ApiGatewayV2StageImporter(IResourceImporterSettings importSettings, ITerraformExportSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencingPropertyPath => "ApiId";

        /// <inheritdoc />
        public override string GetImportId()
        {
            var apiId = this.GetApiId();
            var stageName = this.GetThisResourcePropertyValue("StageName");

            return $"{apiId}/{stageName}";
        }
    }
}