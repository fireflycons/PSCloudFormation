namespace Firefly.PSCloudFormation.Terraform.Importers.Cognito
{
    using System.Linq;

    using Firefly.CloudFormationParser;

    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/cognito_identity_pool_roles_attachment#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class CognitoIdentityPoolRoleAttachmentImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CognitoIdentityPoolRoleAttachmentImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public CognitoIdentityPoolRoleAttachmentImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::Cognito::IdentityPool";

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            // All dependencies that have this attachment as a target
            var dependencies = this.GetResourceDependencies();

            // There should be a 1:1 relationship between attachment and pool.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected identity pool \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport.First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name)
                    .PhysicalId;

                return referencedId;
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
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