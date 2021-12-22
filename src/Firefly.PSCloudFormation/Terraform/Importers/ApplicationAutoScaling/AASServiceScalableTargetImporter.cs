namespace Firefly.PSCloudFormation.Terraform.Importers.ApplicationAutoScaling
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/appautoscaling_target#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    // ReSharper disable once InconsistentNaming
    internal class AASServiceScalableTargetImporter : AbstractAASImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AASServiceScalableTargetImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public AASServiceScalableTargetImporter(IResourceImporterSettings importSettings, ITerraformExportSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => string.Empty;

        /// <inheritdoc />
        protected override string ReferencingPropertyPath => null;

        /// <inheritdoc />
        public override string GetImportId()
        {
            return this.GetAASTarget(this.ImportSettings.Resource);
        }
    }
}