namespace Firefly.PSCloudFormation.Terraform.Importers.ApplicationAutoScaling
{
    using Firefly.PSCloudFormation.Terraform.Hcl;

    // ReSharper disable once StyleCop.SA1600
    // ReSharper disable once InconsistentNaming
    internal abstract class AbstractAASImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractAASImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        protected AbstractAASImporter(IResourceImporterSettings importSettings, ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <summary>
        /// Gets the AAS target (namespace/resource-id/dimension).
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The AAS target identifier.</returns>
        // ReSharper disable once InconsistentNaming
        protected string GetAASTarget(ResourceMapping resource)
        {
            // Manipulate the physical ID to get in the correct format
            var parts = resource.PhysicalId.Split('|');

            var resourceId = parts[0];
            var dimension = parts[1];
            var @namespace = parts[2];

            return $"{@namespace}/{resourceId}/{dimension}";

        }
    }
}
