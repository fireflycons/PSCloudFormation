namespace Firefly.PSCloudFormation.Terraform.Importers.Cognito
{
    using System.Linq;

    using Firefly.CloudFormationParser;

    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/cognito_user_pool_client#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class CognitoUserPoolClientImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CognitoUserPoolClientImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public CognitoUserPoolClientImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::Cognito::UserPool";

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            // All dependencies that have this client as a target
            var dependencies = this.GetResourceDependencies();

            // There should be a 1:1 relationship between pool and client.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected user pool \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport.First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name)
                    .PhysicalId;

                return $"{referencedId}/{this.ImportSettings.Resource.PhysicalId}";
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.LogError(
                    $"Cannot find related user pool client for user group {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.LogError(
                    $"Multiple pool found relating to user group {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (this.TerraformSettings.NonInteractive)
            {
                return null;
            }

            return null;
        }
    }
}