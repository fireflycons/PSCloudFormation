namespace Firefly.PSCloudFormation.Terraform.Importers.Lambda
{
    using System.Linq;
    
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
        protected override string ReferencingPropertyPath => "FunctionName";

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            var dependency = this.GetResourceDependency();

            if (dependency == null)
            {
                return null;
            }

            switch (dependency.DependencyType)
            {
                case DependencyType.Resource:

                    return $"{dependency.Resource.PhysicalId}/{this.ImportSettings.Resource.PhysicalId}";

                case DependencyType.Evaluation:

                    return $"{dependency.PropertyPropertyEvaluation.Split(':').Last()}/{this.ImportSettings.Resource.PhysicalId}";

                default:

                    return null;
            }
        }
    }
}