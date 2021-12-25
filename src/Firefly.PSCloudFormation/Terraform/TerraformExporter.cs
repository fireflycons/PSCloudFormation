namespace Firefly.PSCloudFormation.Terraform
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormationParser.Serialization.Settings;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.Importers;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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
                            r => r.Type == "AWS::Lambda::Function"
                                 && r.GetResourcePropertyValue("Code.ZipFile") != null)).Build();


                // Write out terraform and provider blocks.
                using (var writer = new StreamWriter(HclWriter.MainScriptFile))
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

                        warningCount += new HclWriter(module, this.warnings, this.errors).Serialize(stateFile);

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

            if (File.Exists(HclWriter.VarsFile))
            {
                File.Delete(HclWriter.VarsFile);
            }

            this.settings.Runner.Run("init", true, true, null);
        }

        /// <summary>
        /// Imports the resources by calling <c>terraform import</c> on each.
        /// </summary>
        /// <param name="module">Module containing resources to import.</param>
        /// <returns>List of resources that were imported.</returns>
        private List<ResourceMapping> ImportResources(ModuleInfo module)
        {
            var resourcesToImport = module.ResourceMappings;
            var importedResources = new List<ResourceMapping>();
            var totalResources = resourcesToImport.Count;

            this.settings.Logger.LogInformation(
                $"\nImporting {totalResources} mapped resources from stack \"{module.Settings.StackName}\" to terraform state...");
            var imported = 0;
            
            foreach (var resource in resourcesToImport)
            {
                var resourceToImport = resource.PhysicalId;

                this.settings.Logger.LogInformation($"\nImporting resource {++imported}/{totalResources} - {resource.ImportAddress}");

                if (ResourceImporter.RequiresResourceImporter(resource.TerraformType))
                {
                    var importer = ResourceImporter.Create(
                        new ResourceImporterSettings
                            {
                                Errors = this.errors,
                                Logger = this.settings.Logger,
                                Resource = resource,
                                ResourcesToImport = resourcesToImport.Where(r => r.Module == resource.Module).ToList(), // Only resources in same stack
                                Warnings = this.warnings
                            },
                        module.Settings);

                    if (importer != null)
                    {
                        resourceToImport = importer.GetImportId();

                        if (resourceToImport == null)
                        {
                            continue;
                        }
                    }
                }

                var cmdOutput = new List<string>();
                var success = this.settings.Runner.Run(
                    "import",
                    false,
                    true,
                    msg => cmdOutput.Add(msg),
                    "-no-color",
                    resource.ImportAddress,
                    resourceToImport);

                if (success)
                {
                    importedResources.Add(resource);
                }
                else
                {
                    var error = cmdOutput.FirstOrDefault(o => o.StartsWith("Error: "))?.Substring(7);

                    this.errors.Add(
                        error != null
                            ? $"ERROR: {resource.AwsAddress}: {error}"
                            : $"ERROR: Could not import {resource.AwsAddress}");
                }
            }

            return importedResources;
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
            this.GenerateWarnings();

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

        /// <summary>
        /// Enumerate the resources and issue warnings for those that aren't fully imported..
        /// </summary>
        private void GenerateWarnings()
        {
            // Scan for AWS::Cloudformation::Init metadata and warn about it.
            foreach (var templateResource in this.settings.Template.Resources.Where(
                r => r.Metadata != null && r.Metadata.Keys.Contains("AWS::CloudFormation::Init")))
            {
                this.warnings.Add(
                    $"Resource \"{templateResource.Name}\" contains AWS::CloudFormation::Init metadata which is not imported.");
            }

            // Scan for UserData and warn about it
            var userDataTypes = new[] { "AWS::AutoScaling::LaunchConfiguration", "AWS::EC2::Instance" };

            foreach (var templateResource in this.settings.Template.Resources.Where(
                r => userDataTypes.Contains(r.Type) && r.Properties != null && r.Properties.ContainsKey("UserData")))
            {
                this.warnings.Add($"Resource \"{templateResource.Name}\" contains user data which is not correctly imported.");
            }

            // Scan for lambdas with embedded code (ZipFile) and warn about it
            foreach (var templateResource in this.settings.Template.Resources.Where(
                r => r.Type == "AWS::Lambda::Function" && r.GetResourcePropertyValue("Code.ZipFile") != null))
            {
                this.warnings.Add(
                    $"Resource \"{templateResource.Name}\" contains embedded function code (ZipFile) which may not be the latest version.");
            }

            // Scan for bucket polices and warn
            foreach (var templateResource in this.settings.Template.Resources.Where(r => r.Type == "AWS::S3::BucketPolicy"))
            {
                this.warnings.Add($"Resource \"{templateResource.Name}\" - Policy likely not imported. Add it in manually.");
            }
        }
    }
}