namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.PSCloudFormation.Utils;

    using QuikGraph;

    /// <summary>
    /// Resource importer for user to match a lambda permission with the correct lambda
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class LambdaPermissionImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPermissionImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="dependencyGraph">CloudFormation dependency graph.</param>
        public LambdaPermissionImporter(
            ResourceImport resource,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport,
            IBidirectionalGraph<IVertex, TaggedEdge<IVertex, EdgeDetail>> dependencyGraph)
            : base(resource, ui, resourcesToImport, dependencyGraph)
        {
        }

        /// <summary>
        /// Gets the import identifier.
        /// </summary>
        /// <param name="caption">The caption for the interactive session.</param>
        /// <param name="message">The message for the interactive session.</param>
        /// <returns>
        /// The resource selected by the user, else <c>null</c> if cancelled.
        /// </returns>
        public override string GetImportId(string caption, string message)
        {
            // All dependencies that have this permission as a target
            var dependencies = this.DependencyGraph.Edges
                .Where(e => e.Target.TemplateObject.Name == this.Resource.LogicalId).Where(
                    d => ((IResource)d.Source.TemplateObject).Type == "AWS::Lambda::Function").ToList();

            // There should be a 1:1 relationship between permission and lambda.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.Ui.Information($"Auto-selected lambda function {r.Name} based on dependency graph.");

                return this.ResourcesToImport.First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name)
                    .PhysicalId;
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.Ui.Information($"Cannot find related lambda function for permission {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.Ui.Information($"Multiple lambdas found relating to permission {this.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            var lambdas = this.ResourcesToImport.Where(r => r.AwsType == "AWS::Lambda::Function").ToList();
            var selection = this.SelectResource(
                caption,
                message,
                lambdas.Select(l => l.AwsAddress).ToList());

            return selection == -1 ? null : lambdas[selection].PhysicalId;
        }
    }
}