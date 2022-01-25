namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;
    using Firefly.PSCloudFormation.Terraform.Importers;
    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;

    /// <summary>
    /// Information an a module - a stack in CloudFormation (either root or nested)
    /// </summary>
    [DebuggerDisplay("{FriendlyName}")]
    internal class ModuleInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleInfo"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="nestedModules">The nested modules.</param>
        /// <param name="inputs">List of input variables.</param>
        /// <param name="outputs">List of CloudFormation outputs</param>
        public ModuleInfo(
            ITerraformExportSettings settings,
            IReadOnlyCollection<ModuleInfo> nestedModules,
            IEnumerable<InputVariable> inputs,
            IEnumerable<Output> outputs)
        {
            this.Settings = settings;
            this.NestedModules = nestedModules ?? new List<ModuleInfo>();

            foreach (var nestedModule in this.NestedModules)
            {
                nestedModule.Parent = this;
            }

            if (inputs != null)
            {
                this.Inputs = inputs.ToList();
            }

            if (outputs != null)
            {
                this.Outputs = outputs.ToList();
            }
        }

        /// <summary>
        /// Gets the friendly name for this module.
        /// </summary>
        /// <value>
        /// Friendly name
        /// </value>
        public string FriendlyName => this.IsRootModule ? "<ROOT>" : this.Name;

        /// <summary>
        /// Gets the inputs.
        /// </summary>
        /// <value>
        /// The inputs.
        /// </value>
        public IList<InputVariable> Inputs { get; } = new List<InputVariable>();

        /// <summary>
        /// Gets the CloudFormation outputs.
        /// </summary>
        /// <value>
        /// The outputs.
        /// </value>
        public IReadOnlyCollection<Output> Outputs { get; } = new List<Output>();
        
        /// <summary>
        /// Gets a value indicating whether this module has been imported.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this module has been imported; otherwise, <c>false</c>.
        /// </value>
        public bool IsImported { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is the root module.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is the root module; otherwise, <c>false</c>.
        /// </value>
        public bool IsRootModule => this.Settings.IsRootModule;

        /// <summary>
        /// Gets the absolute path to this module's directory.
        /// </summary>
        /// <value>
        /// The module directory.
        /// </value>
        public string ModuleDirectory => Path.Combine(this.Settings.WorkspaceDirectory, this.Settings.ModuleDirectory);

        /// <summary>
        /// Gets the module name.
        /// For root module, this is null as "module" property is not defined in state file for root resources. 
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => this.IsRootModule ? null : this.Settings.StackName;

        /// <summary>
        /// Gets the logical identifier of the <c>AWS::CloudFormation::Stack</c> that this module represents..
        /// </summary>
        /// <value>
        /// The logical identifier.
        /// </value>
        public string LogicalId => this.Settings.LogicalId;

        /// <summary>
        /// Gets the <see cref="IResource"/> representing the CloudFormation <c>AWS::CloudFormation::Stack</c> that backs this module.
        /// </summary>
        /// <value>
        /// The stack resource.
        /// </value>
        public IResource StackResource => this.Parent?.Settings.Template.Resources.First(r => r.Name == this.LogicalId);

        /// <summary>
        /// Gets the nested modules.
        /// </summary>
        /// <value>
        /// The nested modules.
        /// </value>
        public IReadOnlyCollection<ModuleInfo> NestedModules { get; }

        /// <summary>
        /// Gets the parent module to this one, or <c>null</c> if this is the root module.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public ModuleInfo Parent { get; private set; }

        /// <summary>
        /// Gets the resources discovered from the CloudFormation stack that this module represents.
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        public List<ResourceMapping> ResourceMappings { get; private set; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public ITerraformExportSettings Settings { get; }

        /// <summary>
        /// Get the ancestry of this module back to root.
        /// </summary>
        /// <returns>This module's ancestors.</returns>
        public IEnumerable<ModuleInfo> Ancestors()
        {
            var parent = this.Parent;

            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }

        /// <summary>
        /// Gets a depth-first flattened list of all submodules from this point.
        /// </summary>
        /// <returns>Depth-first flattened list of all submodules from this point.</returns>
        public IEnumerable<ModuleInfo> DescendentsAndThis()
        {
            return this.NestedModules.Flatten(m => m.NestedModules).Concat(new[] { this });
        }

        /// <summary>
        /// Imports the resources by calling <c>terraform import</c> on each.
        /// </summary>
        /// <param name="warnings">Global warnings list</param>
        /// <param name="errors">Global errors list</param>
        /// <returns>List of resources that were imported.</returns>
        public async Task<List<ResourceMapping>> ImportResources(IList<string> warnings, IList<string> errors)
        {
            var importedResources = new List<ResourceMapping>();
            var totalResources = this.ResourceMappings.Count;

            this.Settings.Logger.LogInformation(
                $"\nImporting {totalResources} mapped resources from stack \"{this.Settings.StackName}\" to terraform state...");
            var imported = 0;

            foreach (var resource in this.ResourceMappings)
            {
                var resourceToImport = resource.PhysicalId;

                this.Settings.Logger.LogInformation(
                    $"\nImporting resource {++imported}/{totalResources} - {resource.ImportAddress}");

                if (ResourceImporter.RequiresResourceImporter(resource.TerraformType))
                {
                    var importer = ResourceImporter.Create(
                        new ResourceImporterSettings
                            {
                                Errors = errors,
                                Logger = this.Settings.Logger,
                                Resource = resource,
                                ResourcesToImport =
                                    this.ResourceMappings.Where(r => r.Module == resource.Module)
                                        .ToList(), // Only resources in same stack
                                Warnings = warnings
                            },
                        this.Settings);

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
                var success = this.Settings.Runner.Run(
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

                    errors.Add(
                        error != null
                            ? $"ERROR: {resource.AwsAddress}: {error}"
                            : $"ERROR: Could not import {resource.AwsAddress}");
                }
            }

            this.IsImported = true;

            // Update ancestry
            foreach (var ancestor in this.Ancestors())
            {
                await ancestor.WriteModuleBlocksAsync();
            }

            return importedResources;
        }

        /// <summary>
        /// Processes the AWS resources, mapping them to equivalent Terraform resources and writing out the empty resource declarations.
        /// </summary>
        /// <param name="writer">Output stream to write empty resource declarations to.</param>
        /// <param name="warnings">Global warnings list.</param>
        /// <returns>A list of <see cref="ResourceMapping"/> for successfully mapped AWS resources.</returns>
        public async Task ProcessResources(TextWriter writer, IList<string> warnings)
        {
            var resourcesToImport = new List<ResourceMapping>();
            this.Settings.Logger.LogInformation(
                "- Mapping stack resources to terraform resource types...");

            foreach (var resource in this.Settings.Resources)
            {
                if (TerraformExporterConstants.MergedResources.Contains(resource.StackResource.ResourceType))
                {
                    // Resource is merged into its dependencies.
                    continue;
                }

                if (TerraformExporterConstants.UnsupportedResources.Contains(resource.StackResource.ResourceType))
                {
                    var wrn =
                        $"Resource \"{resource.LogicalResourceId}\" ({resource.ResourceType}): Not supported for import.";
                    this.Settings.Logger.LogWarning(wrn);
                    warnings.Add(wrn);
                    continue;
                }

                var mapping = AwsSchema.ResourceTypeMappings.FirstOrDefault(m => m.Aws == resource.ResourceType);

                if (mapping == null)
                {
                    var wrn =
                        $"Resource \"{resource.LogicalResourceId}\" ({resource.ResourceType}): No corresponding terraform resource.";
                    this.Settings.Logger.LogWarning(wrn);
                    warnings.Add(wrn);
                }
                else
                {
                    await writer.WriteLineAsync(
                        $"resource \"{mapping.Terraform}\" \"{resource.LogicalResourceId}\" {{}}\n");

                    resourcesToImport.Add(
                        new ResourceMapping
                            {
                                PhysicalId = resource.PhysicalResourceId,
                                LogicalId = resource.LogicalResourceId,
                                TerraformType = mapping.Terraform,
                                AwsType = resource.ResourceType,
                                Module = this.Settings.IsRootModule ? null : this
                            });
                }
            }

            this.ResourceMappings = resourcesToImport;
        }

        /// <summary>
        /// Writes the module blocks for any modules this module refers to.
        /// </summary>
        /// <returns>Task to await.</returns>
        public async Task WriteModuleBlocksAsync()
        {
            var modulesFile = Path.Combine(this.ModuleDirectory, TerraformExporterConstants.ModulesFile);

            if (File.Exists(modulesFile))
            {
                // In case we removed all pre-existing submodule references
                File.Delete(modulesFile);
            }

            if (!this.NestedModules.Any())
            {
                return;
            }

            using (var fs = AsyncFileHelpers.OpenWriteAsync(modulesFile))
            using (var writer = new StreamWriter(fs, AsyncFileHelpers.DefaultEncoding))
            {
                foreach (var module in this.NestedModules)
                {
                    var sb = new StringBuilder().AppendLine($"module \"{module.Name}\" {{").AppendLine(
                        $"  source = \"./{module.Settings.ModuleDirectory.Replace('\\', '/')}\"");

                    if (module.IsImported)
                    {
                        // We can only emit module arguments once the module has been imported and
                        // corresponding "variable" declarations are visible to Terraform command.
                        foreach (var input in module.Inputs.Where(i => !(i.IsDataSource || i is PseudoParameterInput)))
                        {
                            sb.AppendLine($"  {input.GenerateVariableAssignment()}");
                        }
                    }

                    sb.AppendLine("}");

                    await writer.WriteLineAsync(sb.ToString());
                }
            }
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
    }
}