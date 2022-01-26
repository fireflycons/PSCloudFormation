namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGatewayV2
{
    using System.Linq;

    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/apigatewayv2_api_mapping#import"/>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class ApiGatewayV2ApiMappingImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayV2ApiMappingImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public ApiGatewayV2ApiMappingImporter(
            IResourceImporterSettings importSettings,
            ITerraformExportSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => string.Empty;

        /// <inheritdoc />
        protected override string ReferencingPropertyPath => string.Empty;

        /// <inheritdoc />
        public override string GetImportId()
        {
            var id = this.ImportSettings.Resource.PhysicalId;
            string domainName = null;

            // Some broad assumptions.
            // Getting the DomainName property from the API Mapping here will resolve to either an actual domain name
            // or the name of an AWS::ApiGatewayV2::DomainName resource
            var propertyValue = this.AwsResource.GetResourcePropertyValue("DomainName");

            if (propertyValue is string domainNameOrResourceName)
            {
                // This will be either a real domain name, or a resource name associated with a !Ref
                if (TerraformExporterConstants.DomainNameRegex.IsMatch(domainNameOrResourceName))
                {
                    domainName = domainNameOrResourceName;
                }
                else
                {
                    var domainResource = this.TerraformSettings.Resources.FirstOrDefault(
                        r => r.TemplateResource.Name == domainNameOrResourceName);

                    if (domainResource != null)
                    {
                        domainName = (string)domainResource.TemplateResource.GetResourcePropertyValue("DomainName");
                    }
                }
            }

            if (domainName != null && TerraformExporterConstants.DomainNameRegex.IsMatch(domainName))
            {
                return $"{id}/{domainName}";
            }

            var warning =
                $"Resource \"{this.AwsResource.Name}\" ({this.AwsResource.Type}): Cannot resolve domain name. This may be a bug - please raise an issue. Resource not imported.";

            this.TerraformSettings.Logger.LogWarning(warning);
            this.ImportSettings.Warnings.Add(warning);
            return null;

        }
    }
}