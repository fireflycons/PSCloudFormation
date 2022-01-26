namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Linq;

    /// <summary>
    /// Imports <c>DOMAIN/BASEPATH</c>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class ApiGatewayBasePathMappingImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayBasePathMappingImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public ApiGatewayBasePathMappingImporter(
            IResourceImporterSettings importSettings,
            ITerraformExportSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::ApiGateway::DomainName";

        /// <inheritdoc />
        protected override string ReferencingPropertyPath => null;

        /// <inheritdoc />
        public override string GetImportId()
        {
            var dependency = this.GetResourceDependency();

            if (dependency == null)
            {
                return null;
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (dependency.DependencyType)
            {
                case DependencyType.Resource:

                    var domain = dependency.Resource.PhysicalId;

                    var basePath =
                        this.TerraformSettings.Template.Resources
                            .First(tr => tr.Name == this.ImportSettings.Resource.LogicalId)
                            .GetResourcePropertyValue("BasePath")?.ToString() ?? string.Empty;

                    return $"{domain}/{basePath}";

                default:

                    return null;
            }
        }
    }
}