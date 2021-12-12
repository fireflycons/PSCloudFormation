namespace Firefly.PSCloudFormation.Terraform.Importers.Lambda
{
    using System.IO;
    using System.Linq;
    using System.Text;

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
        public LambdaFunctionImporter(IResourceImporterSettings importSettings, ITerraformSettings terraformSettings)
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
                return;
            }

            var runtimeObject = templateResource.GetResourcePropertyValue("Runtime");

            if (runtimeObject == null)
            {
                return;
            }

            var runtime = runtimeObject.ToString();
                 
            string extension;

            if (runtime.StartsWith("python"))
            {
                extension = ".py";
            }
            else if (runtime.StartsWith("nodejs"))
            {
                extension = ".js";
            }
            else if (runtime.StartsWith("ruby"))
            {
                extension = ".rb";
            }
            else
            {
                return;
            }

            var dirName = Path.Combine("lambda", this.ImportSettings.Resource.LogicalId);
            Directory.CreateDirectory(dirName);
            var fileName = Path.Combine(dirName, $"index{extension}");

            this.ImportSettings.Logger.LogInformation($"Extracting inline function code to {fileName}");

            File.WriteAllText(
                fileName,
                zipFile.ToString(),
                new UTF8Encoding(false, false));
        }
    }
}