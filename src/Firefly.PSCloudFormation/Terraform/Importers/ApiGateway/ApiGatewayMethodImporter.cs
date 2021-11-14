namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Linq;

    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/api_gateway_method#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ApiGateway.AbstractApiGatewayRestApiImporter" />
    internal class ApiGatewayMethodImporter : AbstractApiGatewayRestApiImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayMethodImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public ApiGatewayMethodImporter(IResourceImporterSettings importSettings, ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencingPropertyPath => "RestApiId";

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            var restApi = this.GetRestApiId();

            if (restApi == null)
            {
                return null;
            }

            var dependency = this.GetResourceDependency("AWS::ApiGateway::Resource");

            if (dependency == null)
            {
                return null;
            }

            switch (dependency.DependencyType)
            {
                case DependencyType.Resource:

                    var httpMethod = this.TerraformSettings.Template.Resources
                        .First(tr => tr.Name == this.ImportSettings.Resource.LogicalId)
                        .GetResourcePropertyValue("HttpMethod")?.ToString();

                    if (httpMethod == null)
                    {
                        return null;
                    }

                    var apiResourceId = dependency.Resource.PhysicalId;

                    return $"{restApi}/{apiResourceId}/{httpMethod}";

                default:

                    return null;
            }
        }
    }
}