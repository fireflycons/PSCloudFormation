namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;

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
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            var dependencies = this.TerraformSettings.Template.DependencyGraph.Edges
                .Where(
                    e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId && e.Source.TemplateObject is IResource
                         && e.Tag != null && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                    d => ((IResource)d.Source.TemplateObject).Type == "AWS::ApiGateway::DomainName").ToList();

            // There should be a 1:1 relationship between attachment and pool.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected API Domain Name \"{r.Name}\" based on dependency graph.");

                var domain = this.ImportSettings.ResourcesToImport
                    .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                var basePath = this.TerraformSettings.Template.Resources.First(tr => tr.Name == this.ImportSettings.Resource.LogicalId)
                    .GetResourcePropertyValue("BasePath")?.ToString() ?? string.Empty;

                return $"{domain}/{basePath}";
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.LogError(
                    $"Cannot find related API Domain Name for {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.LogError(
                    $"Multiple API Domain Names relating to {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;
        }
    }
}