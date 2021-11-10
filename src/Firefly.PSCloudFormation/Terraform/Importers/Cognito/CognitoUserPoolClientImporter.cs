namespace Firefly.PSCloudFormation.Terraform.Importers.Cognito
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    internal class CognitoUserPoolClientImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CognitoUserPoolClientImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        public CognitoUserPoolClientImporter(
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
            // All dependencies that have this client as a target
            var dependencies = this.Settings.Template.DependencyGraph.Edges
                .Where(e => e.Target.TemplateObject.Name == this.Resource.LogicalId && e.Source.TemplateObject is IResource).Where(
                    d => ((IResource)d.Source.TemplateObject).Type == "AWS::Cognito::UserPool").ToList();

            // There should be a 1:1 relationship between pool and client.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.Ui.Information($"Auto-selected user pool \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ResourcesToImport.First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name)
                    .PhysicalId;

                return $"{referencedId}/{this.Resource.PhysicalId}";
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.Ui.Information(
                    $"Cannot find related user pool client for user group {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.Ui.Information(
                    $"Multiple pool found relating to user group {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (this.Settings.NonInteractive)
            {
                return null;
            }

            return null;
        }
    }
}