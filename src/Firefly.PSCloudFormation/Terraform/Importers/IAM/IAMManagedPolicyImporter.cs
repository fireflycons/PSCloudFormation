namespace Firefly.PSCloudFormation.Terraform.Importers.IAM
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/iam_policy#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class IAMManagedPolicyImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IAMManagedPolicyImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public IAMManagedPolicyImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            return $"arn:aws:iam::{this.TerraformSettings.AwsAccountId}:policy/{this.ImportSettings.Resource.PhysicalId}";
        }
    }
}