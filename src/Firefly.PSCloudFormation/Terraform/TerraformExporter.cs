namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation;
    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.State;

    using Newtonsoft.Json;

    /// <summary>
    /// Main class for handling Terraform export
    /// </summary>
    /// <seealso cref="Firefly.EmbeddedResourceLoader.AutoResourceLoader" />
    internal class TerraformExporter : AutoResourceLoader
    {
        /// <summary>
        /// Name of the main script file
        /// </summary>
        private const string MainScriptFile = "main.tf";

        /// <summary>
        /// Name of the Terraform state file
        /// </summary>
        private const string StateFileName = "terraform.tfstate";

        /// <summary>
        /// Map of AWS resource type to Terraform resource type, generated during build.
        /// </summary>
        [EmbeddedResource("terraform-resource-map.json")]
        // ReSharper disable once StyleCop.SA1600 - Loaded by auto-resource
#pragma warning disable 649
        private static string resourceMapJson;
#pragma warning restore 649

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The export settings
        /// </summary>
        private readonly ITerraformSettings settings;

        /// <summary>
        /// The stack resources
        /// </summary>
        private readonly IList<StackResource> stackResources;

        /// <summary>
        /// The AWS stack parameters
        /// </summary>
        private readonly IList<ParameterDeclaration> awsParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerraformExporter"/> class.
        /// </summary>
        /// <param name="stackResources">The stack resources.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="logger">The logger.</param>
        public TerraformExporter(IList<StackResource> stackResources, IList<ParameterDeclaration> parameters, ITerraformSettings settings, ILogger logger)
        {
            this.awsParameters = parameters;
            this.settings = settings;
            this.logger = logger;
            this.stackResources = stackResources;
        }

        /// <summary>
        /// Performs Terraform export.
        /// </summary>
        public void Export()
        {
            var initialHcl = new StringBuilder();
            var finalHcl = new StringBuilder();
            var importFailures = new List<string>();
            var unmappedResources = new List<string>();
            const string TerraformBlock = @"
terraform {
  required_providers {
    aws = {
      source = ""hashicorp/aws""
    }
  }
}

";
            initialHcl.AppendLine(TerraformBlock);
            initialHcl.AppendFormat("provider \"aws\" {{\n  region = \"{0}\"\n}}\n\n", this.settings.AwsRegion);
            finalHcl.AppendLine(TerraformBlock);
            finalHcl.AppendFormat("provider \"aws\" {{\n  region = \"{0}\"\n}}\n\n", this.settings.AwsRegion);

            var parameters = this.ProcessInputVariables();

            foreach (var hclParameter in parameters)
            {
                finalHcl.AppendLine(hclParameter.GenerateHcl());
            }

            var resourcesToImport = this.ProcessResources(initialHcl, unmappedResources);

            if (!resourcesToImport.Any())
            {
                this.logger.LogWarning("No resources were found that could be imported.");
                return;
            }

            // Set up workspace directory
            if (!Directory.Exists(this.settings.WorkspaceDirectory))
            {
                Directory.CreateDirectory(this.settings.WorkspaceDirectory);
            }

            var cwd = Directory.GetCurrentDirectory();
            var importedResources = new List<HclResource>();

            try
            {
                Directory.SetCurrentDirectory(this.settings.WorkspaceDirectory);

                // Write out initial HCL with empty resource declarations, so that terraform import has something to work with
                File.WriteAllText(MainScriptFile, initialHcl.ToString(), new UTF8Encoding(false));

                this.logger.LogInformation("\nInitializing workspace...");
                this.settings.Runner.Run("init", true);
                this.logger.LogInformation("\nImporting mapped resources to terraform state...");

                foreach (var resource in resourcesToImport)
                {
                    if (this.settings.Runner.Run("import", false, resource.Address, resource.PhysicalId))
                    {
                        importedResources.Add(this.settings.Runner.GetResourceDefinition2(resource.Address));
                    }
                    else
                    {
                        importFailures.Add($"{resource.Address} from {resource.AwsAddress}");
                    }
                }

                this.logger.LogInformation("\nResolving dependencies between resources...");
                var stateFile = JsonConvert.DeserializeObject<StateFile>(File.ReadAllText(StateFileName));
                this.ProcessResourceDependencies(stateFile, resourcesToImport, importedResources);

                // Write out HCL 
                foreach (var resource in importedResources)
                {
                    finalHcl.AppendLine(resource.ToString()).AppendLine();
                }

                File.WriteAllText(MainScriptFile, finalHcl.ToString(), new UTF8Encoding(false));
                
                // Now try to fix up the script by running terraform plan until we get no errors or the same errors repeating
                if (this.FixUpScript(parameters))
                {
                    return;
                }

                // If we get here, then the most recent run did not fix anything else
                this.logger.LogWarning(
                    "There are still errors in the script that could not be automatically resolved.");
                this.logger.LogWarning("Run 'terraform plan' to see them and resolve manually.\n");
            }
            finally
            {
                if (unmappedResources.Count > 0)
                {
                    this.logger.LogWarning("Resources that could not be mapped to a corresponding Terraform type:");
                    foreach (var unmappedResource in unmappedResources)
                    {
                        this.logger.LogWarning($"- {unmappedResource}");
                    }

                    this.logger.LogInformation(string.Empty);
                }

                if (importFailures.Count > 0)
                {
                    this.logger.LogWarning("Resources that could not successfully imported:");
                    foreach (var importFailure in importFailures)
                    {
                        this.logger.LogWarning($"- {importFailure}");
                    }

                    this.logger.LogInformation(string.Empty);
                }

                this.logger.LogInformation(
                    "You will still need to make manual edits to the script, for instance to add inputs/outputs or dependencies between resources.");

                Directory.SetCurrentDirectory(cwd);
            }
        }

        /// <summary>
        /// Fixes up the script by adding in the parameters, repeatedly running terraform plan and resolving reported issues until no further fixes can be made
        /// </summary>
        /// <param name="inputVariables">The input variables to add to the script.</param>
        /// <returns><c>true</c> if all issues were resolved; else <c>false</c></returns>
        private bool FixUpScript(IList<InputVariable> inputVariables)
        {
            var passes = 1;
            var script = new HclScript(MainScriptFile);
            script.FixUpVariableReferences(inputVariables);
            script.Save();

            this.logger.LogInformation("\nRunning 'terraform plan' and attempting to fix issues...");
            this.logger.LogInformation("- Pass 1");
            var errors = this.settings.Runner.RunPlan();

            while (true)
            {
                if (errors == null)
                {
                    this.logger.LogInformation("All HCL issues were successfully corrected!");
                    this.logger.LogInformation(string.Empty);
                    return true;
                }

                // Do fixes
                var errorsFixed = errors.Count(error => PlanFixer.Fix(script, error));
                this.logger.LogInformation($"  - Errors fixed: {errorsFixed}/{errors.Count()}");
                script.Save();

                if (errorsFixed == 0)
                {
                    break;
                }

                // Re-run plan
                this.logger.LogInformation($"- Pass {++passes}");
                errors = this.settings.Runner.RunPlan();
            }

            return false;
        }

        /// <summary>
        /// Processes the resource dependencies.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="importedResources">The imported resources.</param>
        private void ProcessResourceDependencies(StateFile stateFile, List<ResourceImport> resourcesToImport, List<HclResource> importedResources)
        {
            var graph = stateFile.GenerateChangeGraph();

            // Walk graph edges and fix up dependencies in HCL script fragments
            foreach (var edge in graph.Edges)
            {
                var dependency = edge.Tag;

                if (dependency == null)
                {
                    this.logger.LogWarning("Untagged graph edge.");
                    continue;
                }

                var sourceResource = resourcesToImport.FirstOrDefault(r => r.Address == edge.Source.Address);
                var targetScript = importedResources.FirstOrDefault(r => r.Address == edge.Target.Address);
                var targetAttribute = dependency.TargetAttribute;

                if (sourceResource == null || targetScript == null || targetAttribute == null)
                {
                    this.logger.LogWarning($"Cannot resolve dependency between {edge.Source.Address} -> {edge.Target.Address}");
                    continue;
                }

                if (dependency.IsArrayMember)
                {
                    targetScript.SetAttributeArrayRefValue(targetAttribute, sourceResource);
                }
                else
                {
                    targetScript.SetAttributeRefValue(targetAttribute, sourceResource);
                }
            }

            // Save the state file in case we added dependencies to resources
            stateFile.Save(StateFileName);
        }

        /// <summary>
        /// Processes the AWS resources, mapping them to equivalent Terraform resources.
        /// </summary>
        /// <param name="initialHcl">The initial HCL.</param>
        /// <param name="unmappedResources">(OUT) AWS resources that were not successfully mapped.</param>
        /// <returns>A list of <see cref="ResourceImport"/> for successfully mapped AWS resources.</returns>
        private List<ResourceImport> ProcessResources(
            StringBuilder initialHcl,
            ICollection<string> unmappedResources)
        {
            var resourceMap = JsonConvert.DeserializeObject<List<ResourceMapping>>(resourceMapJson);
            var resourcesToImport = new List<ResourceImport>();
            this.logger.LogInformation("Processing stack resources and mapping to terraform resource types...");

            foreach (var resource in this.stackResources)
            {
                var mapping = resourceMap.FirstOrDefault(m => m.Aws == resource.ResourceType);

                if (mapping == null)
                {
                    this.logger.LogWarning($"Unable to map resource {resource.LogicalResourceId} ({resource.ResourceType})");
                    unmappedResources.Add($"{resource.LogicalResourceId} ({resource.ResourceType})");
                }
                else
                {
                    initialHcl.AppendLine($"resource \"{mapping.Terraform}\" \"{resource.LogicalResourceId}\" {{}}")
                        .AppendLine();
                    resourcesToImport.Add(
                        new ResourceImport
                            {
                                Address = $"{mapping.Terraform}.{resource.LogicalResourceId}",
                                PhysicalId = resource.PhysicalResourceId,
                                AwsAddress = $"{resource.LogicalResourceId} ({resource.ResourceType})"
                            });
                }
            }

            return resourcesToImport;
        }

        /// <summary>
        /// Processes the input variables.
        /// </summary>
        /// <returns>A list of <see cref="InputVariable"/></returns>
        private List<InputVariable> ProcessInputVariables()
        {
            this.logger.LogInformation("Importing parameters...");
            var parameters = new List<InputVariable>();

            foreach (var p in this.awsParameters)
            {
                var hclParam = InputVariable.CreateParameter(p);

                if (p == null)
                {
                    this.logger.LogWarning($"Cannot import stack parameter '{p.ParameterKey}'");
                }
                else
                {
                    parameters.Add(hclParam);
                }
            }

            return parameters;
        }

        /// <summary>
        /// Maps an AWS resource type to equivalent Terraform resource
        /// </summary>
        [DebuggerDisplay("{Aws} -> {Terraform}")]
        private class ResourceMapping
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
    }
}