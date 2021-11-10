namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Imports <c>DOMAIN/BASEPATH</c>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class ApiGatewayBasePathMappingImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayBasePathMappingImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        public ApiGatewayBasePathMappingImporter(
            ResourceImport resource,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport,
            ITerraformSettings settings)
            : base(resource, ui, resourcesToImport, settings)
        {
        }

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            var dependencies = this.Settings.Template.DependencyGraph.Edges
                .Where(
                    e => e.Target.TemplateObject.Name == this.Resource.LogicalId && e.Source.TemplateObject is IResource
                         && e.Tag != null && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                    d => ((IResource)d.Source.TemplateObject).Type == "AWS::ApiGateway::DomainName").ToList();

            // There should be a 1:1 relationship between attachment and pool.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.Ui.Information($"Auto-selected API Domain Name \"{r.Name}\" based on dependency graph.");

                var domain = this.ResourcesToImport
                    .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                var basePath = this.Settings.Template.Resources.First(tr => tr.Name == this.Resource.LogicalId)
                    .GetResourcePropertyValue("BasePath")?.ToString() ?? string.Empty;

                return $"{domain}/{basePath}";
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.Ui.Information(
                    $"Cannot find related API Domain Name for {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.Ui.Information(
                    $"Multiple API Domain Names relating to {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;
        }
    }
}