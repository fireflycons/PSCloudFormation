namespace Firefly.PSCloudFormation.Terraform.Importers.ApiGateway
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Serves to determine the REST-API-ID that is required to import several other resources.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal abstract class AbstractApiGatewayRestApiImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractApiGatewayRestApiImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        protected AbstractApiGatewayRestApiImporter(
            ResourceImport resource,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport,
            ITerraformSettings settings)
            : base(resource, ui, resourcesToImport, settings)
        {
        }

        /// <summary>
        /// Gets the REST API identifier.
        /// </summary>
        /// <returns>REST API identifier, or <c>null</c> if unresolved.</returns>
        protected string GetRestApiId()
        {
            // All dependencies that have this attachment as a target
            var dependencies = this.Settings.Template.DependencyGraph.Edges
                .Where(
                    e => e.Target.TemplateObject.Name == this.Resource.LogicalId && e.Source.TemplateObject is IResource
                         && e.Tag != null && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                    d => ((IResource)d.Source.TemplateObject).Type == "AWS::ApiGateway::RestApi").ToList();

            // There should be a 1:1 relationship between attachment and pool.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.Ui.Information($"Auto-selected REST API \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ResourcesToImport.First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name)
                    .PhysicalId;

                return referencedId;
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.Ui.Information(
                    $"Cannot find related REST API for {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.Ui.Information(
                    $"Multiple REST APIs relating to {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;

        }
    }
}