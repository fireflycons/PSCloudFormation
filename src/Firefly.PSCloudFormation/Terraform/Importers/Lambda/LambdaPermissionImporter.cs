namespace Firefly.PSCloudFormation.Terraform.Importers.Lambda
{
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics.Functions;

    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/lambda_permission#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class LambdaPermissionImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPermissionImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public LambdaPermissionImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            // All dependencies that have this permission as a target
            var dependencies = this.TerraformSettings.Template.DependencyGraph.Edges
                .Where(e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId && e.Source.TemplateObject is IResource).Where(
                    d => ((IResource)d.Source.TemplateObject).Type == "AWS::Lambda::Function").ToList();

            // There should be a 1:1 relationship between permission and lambda.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected lambda function \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport.First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name)
                    .PhysicalId;

                return $"{referencedId}/{this.ImportSettings.Resource.PhysicalId}";
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                var functionResource =
                    this.TerraformSettings.Template.Resources.First(
                        r => r.Name == this.ImportSettings.Resource.LogicalId);

                var value = functionResource.Properties["FunctionName"];

                if (value is ImportValueIntrinsic intrinsic)
                {
                    this.LogWarning($"Lambda function \"{intrinsic.Evaluate(this.TerraformSettings.Template)}\" for permission \"{this.ImportSettings.Resource.LogicalId}\" is imported from another stack");
                    return null;
                }

                this.LogError(
                    $"Cannot find related lambda function for permission {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.LogError(
                    $"Multiple lambdas found relating to permission {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;
        }
    }
}