namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Linq;

    using Firefly.CloudFormationParser;

    /// <summary>
    /// Serves to determine the REST-API-ID that is required to import several other resources.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal abstract class AbstractApiGatewayRestApiImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractApiGatewayRestApiImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        protected AbstractApiGatewayRestApiImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::ApiGateway::RestApi";

        /// <summary>
        /// Gets the REST API identifier.
        /// </summary>
        /// <returns>REST API identifier, or <c>null</c> if unresolved.</returns>
        protected string GetRestApiId()
        {
            // All dependencies that have this attachment as a target
            var dependencies = this.GetResourceDependencies();

            // There should be a 1:1 relationship between attachment and pool.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected REST API \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport
                    .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                return referencedId;
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.LogError(
                    $"Cannot find related REST API for {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.LogError(
                    $"Multiple REST APIs relating to {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;
        }
    }
}