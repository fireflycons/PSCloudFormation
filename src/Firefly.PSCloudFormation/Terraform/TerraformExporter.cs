namespace Firefly.PSCloudFormation.Terraform
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Amazon.CloudFormation.Model;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Class that manages the end-to-end export of CloudFormation to Terraform
    /// </summary>
    internal class TerraformExporter
    {
        /// <summary>
        /// Path to the Terraform state file
        /// </summary>
        private readonly string stateFilePath;

        /// <summary>
        /// The export settings
        /// </summary>
        private readonly ITerraformExportSettings settings;

        /// <summary>
        /// The accumulated warnings
        /// </summary>
        private readonly List<string> warnings = new List<string>();

        /// <summary>
        /// The accumulated errors
        /// </summary>
        private readonly List<string> errors = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TerraformExporter"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public TerraformExporter(ITerraformExportSettings settings)
        {
            this.settings = settings;
            this.stateFilePath = Path.Combine(settings.WorkspaceDirectory, "terraform.tfstate");
        }

        /// <summary>
        /// Performs Terraform export.
        /// </summary>
        /// <returns><c>true</c> if resources were imported; else <c>false</c></returns>
        public async Task<bool> Export()
        {
            this.settings.Logger.LogInformation("\nInitializing inputs and resources...");

            using (new WorkingDirectoryContext(this.settings.WorkspaceDirectory))
            {
                var configurationBlocks = new ConfigurationBlockBuilder().WithRegion(this.settings.AwsRegion)
                    .WithDefaultTag(this.settings.AddDefaultTag ? this.settings.StackName : null).WithZipper(
                        this.settings.Template.Resources.Any(
                            r => r.Type == TerraformExporterConstants.AwsLambdaFunction
                                 && r.GetResourcePropertyValue(TerraformExporterConstants.LambdaZipFile) != null)).Build();


                // Write out terraform and provider blocks.
                using (var writer = new StreamWriter(TerraformExporterConstants.MainScriptFile))
                {
                    await writer.WriteLineAsync(configurationBlocks);
                }

                // Gather and write out initial stack resources
                var mapper = new ResourceMapper(this.settings, this.warnings);
                var rootModule = await mapper.ProcessStackAsync();

                var allModules = rootModule?.DescendentsAndThis().ToList();

                if (rootModule == null || !allModules.Any())
                {
                    this.settings.Logger.LogWarning("No resources were found that could be imported.");
                    return false;
                }

                var terraformExecutionErrorCount = 0;
                var warningCount = 0;

                try
                {
                    this.InitializeWorkspace();

                    foreach (var module in allModules)
                    {
                        var importedResources = await module.ImportResources(this.warnings, this.errors);

                        if (!this.settings.ExportNestedStacks)
                        {
                            // If leaving nested stacks as aws_cloudformation_stack, then fix up S3 locations of these.
                            await this.FixCloudFormationStacksAsync(importedResources);
                        }

                        var stateFile = JsonConvert.DeserializeObject<StateFile>(
                            await AsyncFileHelpers.ReadAllTextAsync(this.stateFilePath));

                        var writer = new HclWriter(module, this.warnings, this.errors);

                        warningCount += await writer.Serialize(stateFile);

                        this.settings.Logger.LogInformation(
                            $"\nExport of stack \"{module.Settings.StackName}\" to terraform complete!");
                    }
                }
                catch (TerraformRunnerException e)
                {
                    // Errors already reported by output of terraform
                    terraformExecutionErrorCount += e.Errors;
                    warningCount += e.Warnings;
                    throw;
                }
                catch (HclSerializerException e)
                {
                    this.errors.Add(e.Message);
                    throw;
                }
                catch (Exception e)
                {
                    this.errors.Add($"ERROR: Internal error: {e.Message}");
                    throw;
                }
                finally
                {
                    this.WriteSummary(terraformExecutionErrorCount, warningCount);
                }
            }

            return true;
        }

        /// <summary>
        /// Set up the workspace directory, creating if necessary.
        /// Set current directory to workspace directory.
        /// Write HCL block with empty resources ready for import
        /// and run <c>terraform init</c>.
        /// </summary>
        private void InitializeWorkspace()
        {
            this.settings.Logger.LogInformation("\nInitializing workspace...");

            if (File.Exists(TerraformExporterConstants.VarsFile))
            {
                File.Delete(TerraformExporterConstants.VarsFile);
            }

            this.settings.Runner.Run("init", true, true, null);
        }

        /// <summary>
        /// Fixes the imported <c>aws_cloudformation_stack</c> resources by replacing the imported template body with the S3 URL in external state.
        /// </summary>
        /// <param name="importedResources">List of imported resources</param>
        /// <returns>Task to await.</returns>
        private async Task FixCloudFormationStacksAsync(IEnumerable<ResourceMapping> importedResources)
        {
            var changes = new List<StateFileModification>();

            foreach (var importedResource in importedResources.Where(r => r.TerraformType == "aws_cloudformation_stack"))
            {
                // ReSharper disable once StyleCop.SA1305
                var s3Url = this.settings.Resources.FirstOrDefault(r => r.LogicalResourceId == importedResource.LogicalId)
                    ?.TemplateResource.GetResourcePropertyValue("TemplateURL");

                if (s3Url == null)
                {
                    continue;
                }

                // Set template URL
                changes.Add(new StateFileModification(StateFileResourceDeclaration.RootModule, importedResource.LogicalId, "$.template_url", new JValue(s3Url.ToString())));

                // Clear template body
                changes.Add(
                    new StateFileModification(StateFileResourceDeclaration.RootModule, importedResource.LogicalId, "$.template_body", JValue.CreateNull()));

                // Retrieve parameters for stack
                var parameters =
                    (await this.settings.CloudFormationClient.DescribeStacksAsync(
                         new DescribeStacksRequest { StackName = importedResource.PhysicalId })).Stacks.First()
                    .Parameters;

                if (!parameters.Any())
                {
                    continue;
                }

                var parameterBlock = new JObject();

                foreach (var parameter in parameters)
                {
                    // AWS List<> types must be string JValues here, i.e. comma separated,
                    // thus no requirement to assess parameter types and create a JArray.
                    parameterBlock[parameter.ParameterKey] = parameter.ParameterValue;
                }

                changes.Add(new StateFileModification(StateFileResourceDeclaration.RootModule, importedResource.LogicalId, "$.parameters", parameterBlock));
            }

            if (changes.Count > 0)
            {
                await StateFile.UpdateExternalStateFileAsync(changes);
            }
        }

        /// <summary>
        /// Write out the execution summary to console - errors, warnings and recommendations.
        /// </summary>
        /// <param name="terraformExecutionErrorCount">The terraform execution error count.</param>
        /// <param name="warningCount">The warning count.</param>
        private void WriteSummary(int terraformExecutionErrorCount, int warningCount)
        {
            this.settings.Logger.LogInformation("\n");

            var totalErrors = terraformExecutionErrorCount + this.errors.Count;

            if (totalErrors == 0)
            {
                this.warnings.Add(
                    "DO NOT APPLY THIS CONFIGURATION TO AN EXISTING PRODUCTION STACK WITHOUT FIRST THOROUGHLY TESTING ON A COPY.");
            }

            foreach (var error in this.errors)
            {
                this.settings.Logger.LogError(error.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) ? error : $"ERROR: {error}");
            }

            foreach (var warning in this.warnings)
            {
                this.settings.Logger.LogWarning(warning);
            }

            if (this.settings.Resources.Any(r => r.StackResource.ResourceType.StartsWith("Custom::")))
            {
                this.settings.Logger.LogInformation(
                    "\nIt appears this stack contains custom resources. For a suggestion on how to manage these with terraform, see");
                this.settings.Logger.LogInformation("https://trackit.io/trackit-whitepapers/cloudformation-to-terraform-conversion/");
            }

            this.settings.Logger.LogInformation($"\n       Errors: {totalErrors}, Warnings: {warningCount + this.warnings.Count}\n");
        }
    }
}