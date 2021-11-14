namespace Firefly.PSCloudFormation.Terraform.Importers.Lambda
{
    using System.Linq;

    using Firefly.CloudFormationParser;

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
        public LambdaPermissionImporter(IResourceImporterSettings importSettings, ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::Lambda::Function";

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            // All dependencies that have this permission as a target
            var dependencies = this.GetResourceDependencies();

            // There should be a 1:1 relationship between permission and lambda.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected lambda function \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport
                    .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                return $"{referencedId}/{this.ImportSettings.Resource.PhysicalId}";
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                var functionResource =
                    this.TerraformSettings.Template.Resources.First(
                        r => r.Name == this.ImportSettings.Resource.LogicalId);

                if (functionResource.GetResourcePropertyValue("FunctionName") is string importName)
                {
                    var functionArn = this.TerraformSettings.StackExports.FirstOrDefault(e => e.Name == importName)
                        ?.Value;

                    if (functionArn == null)
                    {
                        this.LogWarning(
                            $"Permission \"{this.ImportSettings.Resource.LogicalId}\". Cannot resolve related function which is imported from another stack.");
                        return null;
                    }

                    this.LogWarning(
                        $"Resource \"{this.ImportSettings.Resource.LogicalId}\" references a resource imported from another stack.");
                    return $"{functionArn.Split(':').Last()}/{this.ImportSettings.Resource.PhysicalId}";
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