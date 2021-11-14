namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Linq;

    using Firefly.CloudFormationParser;

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
        public override string GetImportId(string caption, string message)
        {
            var restApi = this.GetRestApiId();

            if (restApi == null)
            {
                return null;
            }

            var dependencies = this.GetResourceDependencies("AWS::ApiGateway::Resource");

            // There should be a 1:1 relationship between attachment and pool.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected ApiResource \"{r.Name}\" based on dependency graph.");

                var httpMethod = this.TerraformSettings.Template.Resources
                    .First(tr => tr.Name == this.ImportSettings.Resource.LogicalId)
                    .GetResourcePropertyValue("HttpMethod")?.ToString();

                if (httpMethod != null)
                {
                    var referencedId = this.ImportSettings.ResourcesToImport
                        .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                    return $"{restApi}/{referencedId}/{httpMethod}";
                }
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.LogError(
                    $"Cannot find related ApiResource for {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.LogError(
                    $"Multiple ApiResources relating to {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;
        }
    }
}