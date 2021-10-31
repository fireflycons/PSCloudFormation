using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Terraform
{
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation;
    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.Importers;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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

#pragma warning disable 649

        /// <summary>
        /// The terraform block
        /// </summary>
        [EmbeddedResource("terraform-block.hcl")]
        // ReSharper disable once StyleCop.SA1600 - Loaded by auto-resource
        private static string terraformBlock;

        /// <summary>
        /// Map of AWS resource type to Terraform resource type, generated during build.
        /// </summary>
        [EmbeddedResource("terraform-resource-map.json")]
        // ReSharper disable once StyleCop.SA1600 - Loaded by auto-resource
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
        /// The UI
        /// </summary>
        private readonly IUserInterface ui;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerraformExporter"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="ui">User interface abstraction for asking questions of the user.</param>
        public TerraformExporter(ITerraformSettings settings, ILogger logger, IUserInterface ui)
        {
            this.settings = settings;
            this.logger = logger;
            this.ui = ui;
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

            initialHcl.AppendLine(terraformBlock.Replace("#REGION#", this.settings.AwsRegion));
            finalHcl.AppendLine(terraformBlock.Replace("#REGION#", this.settings.AwsRegion));

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
            var importedResources = new List<ResourceImport>();

            try
            {
                Directory.SetCurrentDirectory(this.settings.WorkspaceDirectory);

                // Write out initial HCL with empty resource declarations, so that terraform import has something to work with
                File.WriteAllText(MainScriptFile, initialHcl.ToString(), new UTF8Encoding(false));

                this.logger.LogInformation("\nInitializing workspace...");
                this.settings.Runner.Run("init", true, null);
                this.logger.LogInformation("\nImporting mapped resources to terraform state...");

                foreach (var resource in resourcesToImport)
                {
                    var commandOutput = new List<string>();

                    var success = this.settings.Runner.Run(
                        "import",
                        false,
                        msg => commandOutput.Add(msg),
                        resource.Address,
                        resource.PhysicalId);

                    if (success)
                    {
                        importedResources.Add(resource);
                    }
                    else if (!this.settings.NonInteractive)
                    {
                        // Likely on a lambda permission
                        if (commandOutput.ContainsText("Unexpected format of ID"))
                        {
                            var importer = ResourceImporter.Create(resource.TerraformType, this.ui, resourcesToImport);

                            if (importer != null)
                            {
                                var target = importer.GetImportId(
                                    $"Select related resource for {resource.AwsAddress}",
                                    null);

                                success = this.settings.Runner.Run(
                                    "import",
                                    false,
                                    null,
                                    resource.Address,
                                    $"{target}/{resource.PhysicalId}");

                                if (success)
                                {
                                    importedResources.Add(resource);
                                    continue;
                                }
                            }
                        }
                        
                        importFailures.Add($"{resource.Address} from {resource.AwsAddress}");
                    }
                }

                // Now start adjusting the state file in accordance with the template's directed edge graph.
                this.logger.LogInformation("\nResolving dependencies between resources...");

                // One copy of the state file that we will modify and write back out (adding dependencies and sensitive attributes)
                var stateFile = JsonConvert.DeserializeObject<StateFile>(File.ReadAllText(StateFileName));

                // TODO: Analyze state file for null properties that have defaults. Replace these defaults and write back out.

                // A second copy of the state file that we will insert references to inputs, other resources etc. before serialization to HCL.
                var hclGeneratationStateFile = JsonConvert.DeserializeObject<StateFile>(File.ReadAllText(StateFileName));

                // Wire up dependencies
                foreach (var edge in this.settings.Template.DependencyGraph.Edges.Where(e => e.Source is ResourceVertex && e.Target is ResourceVertex))
                {
                    var sourceAwsResource = this.settings.Resources.First(r => r.LogicalResourceId == edge.Source.Name);
                    var targetAwsResource = this.settings.Resources.First(r => r.LogicalResourceId == edge.Target.Name);

                    var sourceTerraformResource =
                        importedResources.FirstOrDefault(r => sourceAwsResource.LogicalResourceId == r.LogicalId);
                    var targetTerraformResource =
                        importedResources.FirstOrDefault(r => targetAwsResource.LogicalResourceId == r.LogicalId);

                    if (sourceTerraformResource == null || targetTerraformResource == null)
                    {
                        // Resource wasn't imported
                        continue;
                    }

                    var sourceState = hclGeneratationStateFile.Resources.First(r => r.Address == sourceTerraformResource.Address)
                        .Instances.First();
                    var targetState = hclGeneratationStateFile.Resources.First(r => r.Address == targetTerraformResource.Address)
                        .Instances.First();

                    if (edge.Tag?.ReferenceType == ReferenceType.DirectReference)
                    {
                        var newValue = new DirectReference(sourceTerraformResource.Address);
                        var con = new JConstructor(newValue.ReferenceExpression, newValue);
                        foreach (var node in targetState.FindId(sourceAwsResource.PhysicalResourceId))
                        {
                            switch (node)
                            {
                                case JProperty jp when jp.Value is JValue:

                                    jp.Value = con;
                                    break;

                                case JArray ja:

                                    for (var ind = 0; ind < ja.Count; ++ind)
                                    {
                                        var stringVal = ja[ind].Value<string>();

                                        if (stringVal == sourceAwsResource.PhysicalResourceId)
                                        {
                                            ja.RemoveAt(ind);
                                            ja.Add(con);
                                        }
                                    }

                                    break;
                            }
                        }
                    }
                }

                // Serialize to HCL
                using (var s = new StringWriter())
                {
                    var serializer = new Serializer(new HclEmitter(s));
                    serializer.Serialize(hclGeneratationStateFile);

                    var hcl = s.ToString();

                    var main = Path.Combine(this.settings.WorkspaceDirectory, "main.tf");

                    if (File.Exists(main))
                    {
                        File.Delete(main);
                    }

                    var enc = new UTF8Encoding(false);
                    File.WriteAllText(main, terraformBlock.Replace("#REGION#", this.settings.AwsRegion), enc);
                    File.AppendAllText(main, hcl, enc);
                }
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
        /// Processes the input variables.
        /// </summary>
        /// <returns>A list of <see cref="InputVariable"/></returns>
        private List<InputVariable> ProcessInputVariables()
        {
            this.logger.LogInformation("Importing parameters...");
            var parameters = new List<InputVariable>();

            foreach (var p in this.settings.Template.Parameters)
            {
                var hclParam = InputVariable.CreateParameter(p);

                if (hclParam == null)
                {
                    this.logger.LogWarning($"Cannot import stack parameter '{p.Name}'");
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
        /// <param name="unmappedResources">(OUT) AWS resources that were not successfully mapped.</param>
        /// <returns>A list of <see cref="ResourceImport"/> for successfully mapped AWS resources.</returns>
        private List<ResourceImport> ProcessResources(
            StringBuilder initialHcl,
            ICollection<string> unmappedResources)
        {
            var resourceMap = JsonConvert.DeserializeObject<List<ResourceMapping>>(resourceMapJson);
            var resourcesToImport = new List<ResourceImport>();
            this.logger.LogInformation("Processing stack resources and mapping to terraform resource types...");

            foreach (var resource in this.settings.Resources)
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
