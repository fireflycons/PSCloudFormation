namespace Firefly.PSCloudFormation.Terraform.Importers.ECS
{
    using System.Linq;

    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/ecs_service#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    // ReSharper disable once InconsistentNaming
    internal class ECSServiceImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ECSServiceImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public ECSServiceImporter(IResourceImporterSettings importSettings, ITerraformSettings terraformSettings)
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
            // Just need to manipulate the ARN a bit here. 
            return string.Join("/", this.ImportSettings.Resource.PhysicalId.Split('/').Skip(1));
        }
    }
}