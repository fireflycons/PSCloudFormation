namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGatewayV2
{
    /// <summary>
    /// Serves to determine the API-ID that is required to import several other resources.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal abstract class AbstractApiGatewayV2ApiImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractApiGatewayV2ApiImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        protected AbstractApiGatewayV2ApiImporter(
            IResourceImporterSettings importSettings,
            ITerraformExportSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::ApiGatewayV2::Api";

        /// <summary>
        /// Gets the REST API identifier.
        /// </summary>
        /// <returns>REST API identifier, or <c>null</c> if unresolved.</returns>
        protected string GetApiId()
        {
            var dependency = this.GetResourceDependency();

            if (dependency == null)
            {
                return null;
            }

            switch (dependency.DependencyType)
            {
                case DependencyType.Resource:

                    return dependency.Resource.PhysicalId;

                case DependencyType.Evaluation:

                    return dependency.PropertyPropertyEvaluation;

                default:

                    return null;
            }
        }
    }
}