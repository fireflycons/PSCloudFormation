namespace Firefly.PSCloudFormation.Terraform.Importers.Lambda
{
    using System.IO;
    using System.Linq;
    using System.Text;

    using Firefly.PSCloudFormation.LambdaPackaging;

    /// <summary>
    /// Imports a lambda function, extracting any inline function code to directory <c>lambda</c>.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class LambdaFunctionImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaFunctionImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public LambdaFunctionImporter(IResourceImporterSettings importSettings, ITerraformExportSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => string.Empty;

        /// <inheritdoc />
        protected override string ReferencingPropertyPath => string.Empty;

        /// <inheritdoc />
        public override string GetImportId()
        {
            this.ExtractZipFileCode();
            return this.ImportSettings.Resource.PhysicalId;
        }

        /// <summary>
        /// If the lambda has embedded code, extract it to a file.
        /// </summary>
        private void ExtractZipFileCode()
        {
            var templateResource = this.TerraformSettings.Resources.First(
                r => r.LogicalResourceId == this.ImportSettings.Resource.LogicalId).TemplateResource;

            var zipFile = templateResource.GetResourcePropertyValue("Code.ZipFile");

            if (zipFile == null)
            {
                // No embedded script
                return;
            }

            var runtimeObject = templateResource.GetResourcePropertyValue("Runtime");

            if (runtimeObject == null)
            {
                // No runtime specified
                return;
            }

            var traits = LambdaTraits.FromRuntime(runtimeObject.ToString());

            var dirName = Path.Combine("lambda", this.ImportSettings.Resource.LogicalId);
            Directory.CreateDirectory(dirName);
            var fileName = Path.Combine(dirName, $"index{traits.ScriptFileExtension}");

            this.ImportSettings.Logger.LogInformation($"Extracting inline function code to {fileName}");

            File.WriteAllText(
                fileName,
                zipFile.ToString(),
                new UTF8Encoding(false, false));
        }
    }
}