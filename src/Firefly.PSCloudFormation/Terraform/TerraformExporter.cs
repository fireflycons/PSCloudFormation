namespace Firefly.PSCloudFormation.Terraform
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormation;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.Importers;
    using Firefly.PSCloudFormation.Terraform.State;

    using Newtonsoft.Json;

    internal partial class TerraformExporter : AutoResourceLoader
    {
        /// <summary>
        /// These resources have no direct terraform representation.
        /// They are merged into the resources that depend on them
        /// when the dependent resource is imported.
        /// </summary>
        public static readonly List<string> MergedResources = new List<string>
                                                                  {
                                                                      "AWS::CloudFront::CloudFrontOriginAccessIdentity",
                                                                      "AWS::IAM::Policy",
                                                                      "AWS::EC2::SecurityGroupIngress",
                                                                      "AWS::EC2::SecurityGroupEgress",
                                                                      "AWS::EC2::VPCGatewayAttachment",
                                                                      "AWS::EC2::SubnetNetworkAclAssociation",
                                                                      "AWS::EC2::NetworkAclEntry"
                                                                  };

        /// <summary>
        /// These resources are currently not supported for import.
        /// </summary>
        public static readonly List<string> UnsupportedResources = new List<string> { "AWS::ApiGateway::Deployment" };

        /// <summary>
        /// Combination of merged and unsupported resources.
        /// </summary>
        public static readonly List<string> IgnoredResources = MergedResources.Concat(UnsupportedResources).ToList();

        /// <summary>
        /// Name of the Terraform state file
        /// </summary>
        private const string StateFileName = "terraform.tfstate";

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The export settings
        /// </summary>
        private readonly ITerraformSettings settings;

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
        /// <param name="logger">The logger.</param>
        public TerraformExporter(ITerraformSettings settings, ILogger logger)
        {
            this.settings = settings;
            this.logger = logger;
        }

        /// <summary>
        /// Performs Terraform export.
        /// </summary>
        public void Export()
        {
            var initialHcl = new StringBuilder();

            this.logger.LogInformation("\nInitializing inputs and resources...");

            initialHcl.AppendLine(
                HclWriter.GetTerraformBlock(
                    this.settings.AwsRegion,
                    this.settings.AddDefaultTag ? this.settings.StackName : null));

            var parameters = this.ProcessInputVariables();

            var resourcesToImport = this.ProcessResources(initialHcl);

            if (!resourcesToImport.Any())
            {
                this.logger.LogWarning("No resources were found that could be imported.");
                return;
            }

            var cwd = Directory.GetCurrentDirectory();
            var terraformExecutionErrorCount = 0;
            var warningCount = 0;

            try
            {
                this.InitializeWorkspace(initialHcl.ToString());

                //var importedResources = resourcesToImport.Where(r => r.AwsType != "AWS::SecretsManager::SecretTargetAttachment").ToList();
                var importedResources = this.ImportResources(resourcesToImport);

                // TODO: Fix up lambda S3 sources.
                // TODO: Extract inline lambda code to files. Add "ArthurHlt/Zipper" provider as source for the file.
                // TODO: Analyze state file for null properties that have defaults. Replace these defaults and write back out.

                // Copy of the state file that we will insert references to inputs, other resources etc. before serialization to HCL.
                var stateFile = JsonConvert.DeserializeObject<StateFile>(File.ReadAllText(StateFileName));

                warningCount += new HclWriter(this.settings, this.logger).Serialize(
                    stateFile,
                    importedResources,
                    parameters);

                this.logger.LogInformation($"Export of stack \"{this.settings.StackName}\" to terraform complete!");
            }
            catch (TerraformRunnerException e)
            {
                terraformExecutionErrorCount += e.Errors;
                warningCount += e.Warnings;
                throw;
            }
            catch (HclSerializerException)
            {
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
                Directory.SetCurrentDirectory(cwd);
            }
        }

        /// <summary>
        /// Set up the workspace directory, creating if necessary.
        /// Set current directory to workspace directory.
        /// Write HCL block with empty resources ready for import
        /// and run <c>terraform init</c>.
        /// </summary>
        /// <param name="initialHcl">Initial HCL to write to workspace</param>
        private void InitializeWorkspace(string initialHcl)
        {
            this.logger.LogInformation("\nInitializing workspace...");

            // Set up workspace directory
            if (!Directory.Exists(this.settings.WorkspaceDirectory))
            {
                Directory.CreateDirectory(this.settings.WorkspaceDirectory);
            }

            Directory.SetCurrentDirectory(this.settings.WorkspaceDirectory);

            if (File.Exists(HclWriter.VarsFile))
            {
                File.Delete(HclWriter.VarsFile);
            }

            // Write out initial HCL with empty resource declarations, so that terraform import has something to work with
            File.WriteAllText(HclWriter.MainScriptFile, initialHcl, new UTF8Encoding(false));

            this.settings.Runner.Run("init", true, null);
        }

        /// <summary>
        /// Imports the resources by calling <c>terraform import</c> on each.
        /// </summary>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <returns>List of resources that were imported.</returns>
        private List<ResourceMapping> ImportResources(List<ResourceMapping> resourcesToImport)
        {
            List<ResourceMapping> importedResources = new List<ResourceMapping>();
            var totalResources = resourcesToImport.Count;

            this.logger.LogInformation(
                $"\nImporting {totalResources} mapped resources from stack \"{this.settings.StackName}\" to terraform state...");
            var imported = 0;
            
            foreach (var resource in resourcesToImport)
            {
                var resourceToImport = resource.PhysicalId;

                this.logger.LogInformation($"\nImporting resource {++imported}/{totalResources} - {resource.Address}");

                if (ResourceImporter.RequiresResourceImporter(resource.TerraformType))
                {
                    var importer = ResourceImporter.Create(
                        new ResourceImporterSettings
                            {
                                Errors = this.errors,
                                Logger = this.logger,
                                Resource = resource,
                                ResourcesToImport = resourcesToImport,
                                Warnings = this.warnings
                            },
                        this.settings);

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
                    msg => cmdOutput.Add(msg),
                    "-no-color",
                    resource.Address,
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
        /// Write out the execution summary to console - errors, warnings and recommendations.
        /// </summary>
        /// <param name="terraformExecutionErrorCount">The terraform execution error count.</param>
        /// <param name="warningCount">The warning count.</param>
        private void WriteSummary(int terraformExecutionErrorCount, int warningCount)
        {
            this.logger.LogInformation("\n");
            this.GenerateWarnings();

            var totalErrors = terraformExecutionErrorCount + this.errors.Count;

            if (totalErrors == 0)
            {
                this.warnings.Add(
                    "DO NOT APPLY THIS CONFIGURATION TO AN EXISTING PRODUCTION STACK WITHOUT FIRST THOROUGHLY TESTING ON A COPY.");
            }

            foreach (var error in this.errors)
            {
                this.logger.LogError(error);
            }

            foreach (var warning in this.warnings)
            {
                this.logger.LogWarning(warning);
            }

            if (this.settings.Resources.Any(r => r.StackResource.ResourceType.StartsWith("Custom::")))
            {
                this.logger.LogInformation(
                    "\nIt appears this stack contains custom resources. For a suggestion on how to manage these with terraform, see");
                this.logger.LogInformation("https://trackit.io/trackit-whitepapers/cloudformation-to-terraform-conversion/");
            }

            if (this.settings.Template.Resources.Any(
                r => r.Type == "AWS::Lambda::Function" && r.GetResourcePropertyValue("Code.ZipFile") != null))
            {
                this.logger.LogInformation(
                    "\nLambdas with template-embedded code were detected. Manage these by extracting code to a file,");
                this.logger.LogInformation("and using the \"filename\" argument of the lambda resource to reference it.");
            }

            this.logger.LogInformation($"\n       Errors: {totalErrors}, Warnings: {warningCount + this.warnings.Count}\n");
        }

        /// <summary>
        /// Enumerate the resources and issue warnings for those that aren't fully imported..
        /// </summary>
        private void GenerateWarnings()
        {
            var zipFileDetected = false;

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
                    $"Resource \"{templateResource.Name}\" contains embedded function code (ZipFile) which is not imported.");
                zipFileDetected = true;
            }

            // Scan for bucket polices and warn
            foreach (var templateResource in this.settings.Template.Resources.Where(r => r.Type == "AWS::S3::BucketPolicy"))
            {
                this.warnings.Add($"Resource \"{templateResource.Name}\" - Policy likely not imported. Add it in manually.");
            }
        }

        /// <summary>
        /// Processes the input variables.
        /// </summary>
        /// <returns>A list of <see cref="InputVariable"/></returns>
        private List<InputVariable> ProcessInputVariables()
        {
            this.logger.LogInformation("Importing parameters...");
            var parameters = new List<InputVariable>();

            foreach (var p in this.settings.Template.Parameters.Concat(this.settings.Template.PseudoParameters))
            {
                var hclParam = InputVariable.CreateParameter(p);

                if (hclParam == null)
                {
                    var wrn = p is PseudoParameter
                                  ? $"Pseudo-parameter '{p.Name}' cannot be imported as it is not supported by terraform."
                                  : $"Stack parameter '{p.Name}' cannot be imported.";

                    this.logger.LogWarning(wrn);
                    this.warnings.Add(wrn);
                }
                else
                {
                    parameters.Add(hclParam);
                }
            }

            return parameters;
        }

        /// <summary>
        /// Processes the AWS resources, mapping them to equivalent Terraform resources.
        /// </summary>
        /// <param name="initialHcl">The initial HCL.</param>
        /// <returns>A list of <see cref="ResourceMapping"/> for successfully mapped AWS resources.</returns>
        private List<ResourceMapping> ProcessResources(StringBuilder initialHcl)
        {
            var resourceMap = JsonConvert.DeserializeObject<List<ResourceTypeMapping>>(resourceMapJson);
            var resourcesToImport = new List<ResourceMapping>();
            this.logger.LogInformation("Processing stack resources and mapping to terraform resource types...");

            foreach (var resource in this.settings.Resources)
            {
                if (MergedResources.Contains(resource.StackResource.ResourceType))
                {
                    // Resource is merged into its dependencies.
                    continue;
                }

                if (UnsupportedResources.Contains(resource.StackResource.ResourceType))
                {
                    var wrn =
                        $"Resource \"{resource.LogicalResourceId}\" ({resource.ResourceType}): Not supported for import.";
                    this.logger.LogWarning(wrn);
                    this.warnings.Add(wrn);
                    continue;
                }

                var mapping = resourceMap.FirstOrDefault(m => m.Aws == resource.ResourceType);

                if (mapping == null)
                {
                    var wrn =
                        $"Resource \"{resource.LogicalResourceId}\" ({resource.ResourceType}): No corresponding terraform resource.";
                    this.logger.LogWarning(wrn);
                    this.warnings.Add(wrn);
                }
                else
                {
                    initialHcl.AppendLine($"resource \"{mapping.Terraform}\" \"{resource.LogicalResourceId}\" {{}}")
                        .AppendLine();
                    resourcesToImport.Add(
                        new ResourceMapping
                            {
                                PhysicalId = resource.PhysicalResourceId,
                                LogicalId = resource.LogicalResourceId,
                                TerraformType = mapping.Terraform,
                                AwsType = resource.ResourceType
                            });
                }
            }

            return resourcesToImport;
        }

        /// <summary>
        /// Maps an AWS resource type to equivalent Terraform resource.
        /// This is backed by the embedded resource <c>terraform-resource-map.json</c>
        /// </summary>
        [DebuggerDisplay("{Aws} -> {Terraform}")]
        private class ResourceTypeMapping
        {
            /// <summary>
            /// Gets or sets the AWS type name.
            /// </summary>
            /// <value>
            /// The AWS type name.
            /// </value>
            [JsonProperty("AWS")]
            public string Aws { get; set; }

            /// <summary>
            /// Gets or sets the terraform type name.
            /// </summary>
            /// <value>
            /// The terraform.
            /// </value>
            [JsonProperty("TF")]
            public string Terraform { get; set; }
        }

#pragma warning disable 649

        /// <summary>
        /// Map of AWS resource type to Terraform resource type, generated during build.
        /// </summary>
        [EmbeddedResource("terraform-resource-map.json")]

        // ReSharper disable once StyleCop.SA1600 - Loaded by auto-resource
        private static string resourceMapJson;
#pragma warning restore 649
    }
}