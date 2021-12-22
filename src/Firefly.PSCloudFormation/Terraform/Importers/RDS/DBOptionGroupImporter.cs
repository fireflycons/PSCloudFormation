namespace Firefly.PSCloudFormation.Terraform.Importers.RDS
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/db_option_group#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    // ReSharper disable once InconsistentNaming
    internal class DBOptionGroupImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DBOptionGroupImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public DBOptionGroupImporter(IResourceImporterSettings importSettings, ITerraformExportSettings terraformSettings)
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
            // DB option group names are always lowercase
            return this.ImportSettings.Resource.PhysicalId.ToLowerInvariant();
        }
    }
}