namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    internal class ApiGatewayMethodImporter : AbstractApiGatewayRestApiImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGatewayMethodImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        public ApiGatewayMethodImporter(
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
            var restApi = this.GetRestApiId();

            if (restApi == null)
            {
                return null;
            }

            var dependencies = this.Settings.Template.DependencyGraph.Edges
                .Where(
                    e => e.Target.TemplateObject.Name == this.Resource.LogicalId && e.Source.TemplateObject is IResource
                         && e.Tag != null && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                    d => ((IResource)d.Source.TemplateObject).Type == "AWS::ApiGateway::Resource").ToList();

            // There should be a 1:1 relationship between attachment and pool.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.Ui.Information($"Auto-selected ApiResource \"{r.Name}\" based on dependency graph.");

                var httpMethod = this.Settings.Template.Resources.First(tr => tr.Name == this.Resource.LogicalId)
                    .GetResourcePropertyValue("HttpMethod")?.ToString();

                if (httpMethod != null)
                {
                    var referencedId = this.ResourcesToImport
                        .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                    return $"{restApi}/{referencedId}/{httpMethod}";
                }
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.Ui.Information(
                    $"Cannot find related ApiResource for {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.Ui.Information(
                    $"Multiple ApiResources relating to {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;
        }
    }
}