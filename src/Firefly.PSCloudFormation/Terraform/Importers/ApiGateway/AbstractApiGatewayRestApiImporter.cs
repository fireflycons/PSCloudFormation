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