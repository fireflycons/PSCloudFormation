namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.PSCloudFormation.LambdaPackaging;
    using Firefly.PSCloudFormation.Terraform.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.DependencyResolver;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.Importers.Lambda;
    using Firefly.PSCloudFormation.Terraform.State;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Controls the complete serialization process of the state file to HCL.
    /// </summary>
    internal class HclWriter
    {
        private static readonly Dictionary<string, string> PlanActions = new Dictionary<string, string>
                                                                             {
                                                                                 { "create", "will be created." },
                                                                                 { "delete", "will be DESTROYED!" },
                                                                                 {
                                                                                     "update",
                                                                                     "will be updated in-place."
                                                                                 },
                                                                                 { "replace", "will be REPLACED!" }
                                                                             };

        /// <summary>
        /// The error list
        /// </summary>
        private readonly IList<string> errors;

        /// <summary>
        /// The module to serialize
        /// </summary>
        private readonly ModuleInfo module;

        /// <summary>
        /// The settings
        /// </summary>
        private readonly ITerraformExportSettings settings;

        /// <summary>
        /// The warning list
        /// </summary>
        private readonly IList<string> warnings;

        /// <summary>
        /// Initializes a new instance of the <see cref="HclWriter"/> class.
        /// </summary>
        /// <param name="module">The module to serialize.</param>
        /// <param name="warnings">Warning list</param>
        /// <param name="errors">Error list</param>
        public HclWriter(ModuleInfo module, IList<string> warnings, IList<string> errors)
        {
            this.module = module;
            this.errors = errors;
            this.warnings = warnings;
            this.settings = module.Settings;
        }

        /// <summary>
        /// Generate and write out all HCL.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        /// <returns>Count of validation warnings.</returns>
        public async Task<int> Serialize(StateFile stateFile)
        {
            await this.WriteMain(stateFile);
            this.WriteTfVars();
            this.GenerateWarnings(stateFile);

            return this.settings.IsRootModule ? this.FormatAndValidateOutput() : 0;
        }

        /// <summary>
        /// Resolves lambda s3 code location.
        /// </summary>
        /// <param name="cloudFormationResource">The cloud formation resource.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="mapping">The mapping.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <param name="codeDefinition">The code definition.</param>
        private static void ResolveLambdaS3Code(
            ITemplateObject cloudFormationResource,
            JObject attributes,
            ResourceMapping mapping,
            IList<InputVariable> inputs,
            IDictionary<string, object> codeDefinition)
        {
            // Must contain S3Key and S3Bucket, and optionally S3ObjectVersion
            if (!(codeDefinition.ContainsKey("S3Bucket") && codeDefinition.ContainsKey("S3Key")))
            {
                return;
            }

            UpdatePropertyValue(
                "s3_bucket",
                cloudFormationResource.Template,
                attributes,
                mapping,
                inputs,
                codeDefinition["S3Bucket"]);
            UpdatePropertyValue(
                "s3_key",
                cloudFormationResource.Template,
                attributes,
                mapping,
                inputs,
                codeDefinition["S3Key"]);

            if (codeDefinition.ContainsKey("S3ObjectVersion"))
            {
                UpdatePropertyValue(
                    "s3_object_version",
                    cloudFormationResource.Template,
                    attributes,
                    mapping,
                    inputs,
                    codeDefinition["S3ObjectVersion"]);
            }
        }

        /// <summary>
        /// Resolves the lambda ZipFile code.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="runtime">The runtime.</param>
        /// <param name="cloudFormationResource">The cloud formation resource.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="mapping">The mapping.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        private static void ResolveLambdaZipCode(
            TextWriter writer,
            string runtime,
            ITemplateObject cloudFormationResource,
            JObject attributes,
            ResourceMapping mapping,
            IList<InputVariable> inputs)
        {
            var traits = LambdaTraits.FromRuntime(runtime);

            var dirName = Path.Combine("lambda", cloudFormationResource.Name);
            var fileName = Path.Combine(dirName, $"index{traits.ScriptFileExtension}");
            var zipName = Path.Combine(dirName, $"{cloudFormationResource.Name}_deployment_package.zip");

            // Create zipper resource
            var zipperResource = $"{cloudFormationResource.Name}_deployment_package";

            var archiveDataSource = new DataSourceInput(
                "archive_file",
                zipperResource,
                new Dictionary<string, string>
                    {
                        { "type", "zip" },
                        { "source_file", fileName.Replace("\\", "/") },
                        { "output_path", zipName.Replace("\\", "/") }
                    });

            inputs.Add(archiveDataSource);

            // Fix up state file.
            // Find handler function
            var script = File.ReadAllText(fileName);
            var m = traits.HandlerRegex.Match(script);

            if (m.Success)
            {
                UpdatePropertyValue(
                    "handler",
                    cloudFormationResource.Template,
                    attributes,
                    mapping,
                    inputs,
                    $"index.{m.Groups["handler"].Value}");
            }

            UpdatePropertyValue(
                "filename",
                cloudFormationResource.Template,
                attributes,
                mapping,
                inputs,
                new DataSourceReference(
                    "archive_file",
                    $"{cloudFormationResource.Name}_deployment_package",
                    "output_path",
                    false));

            UpdatePropertyValue(
                "source_code_hash",
                cloudFormationResource.Template,
                attributes,
                mapping,
                inputs,
                new DataSourceReference(
                    "archive_file",
                    $"{cloudFormationResource.Name}_deployment_package",
                    "output_base64sha256",
                    false));
        }

        /// <summary>
        /// Updates a property value in the in-memory state file..
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="template">The template.</param>
        /// <param name="attributes">The attributes of the resource to update.</param>
        /// <param name="resourceMapping">The resource mapping.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <param name="newValue">The new value.</param>
        private static void UpdatePropertyValue(
            string key,
            ITemplate template,
            JObject attributes,
            ResourceMapping resourceMapping,
            IList<InputVariable> inputs,
            object newValue)
        {
            JToken newJToken;

            switch (newValue)
            {
                case IIntrinsic intrinsic:

                    newJToken = intrinsic.Render(template, resourceMapping, inputs).ToJConstructor();
                    break;

                case Reference reference:

                    newJToken = reference.ToJConstructor();
                    break;

                case JToken tok:

                    newJToken = tok;
                    break;

                default:

                    newJToken = new JValue(newValue.ToString());
                    break;
            }

            var oldValue = attributes.SelectToken(key);

            if (oldValue != null)
            {
                // ReSharper disable once PossibleNullReferenceException - It should be found as all attributes should be in state file
                ((JProperty)oldValue.Parent).Value = newJToken;
                return;
            }

            attributes.Add(key, newJToken);
        }

        /// <summary>
        /// Writes the outputs section of <c>main.tf</c>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
        /// <param name="outputs">The outputs.</param>
        private static void WriteOutputs(TextWriter writer, IEnumerable<OutputValue> outputs)
        {
            foreach (var output in outputs)
            {
                writer.WriteLine(output.GenerateHcl());
            }
        }

        /// <summary>
        /// Formats and validates the generated output using <c>terraform fmt</c> and <c>terraform validate</c>.
        /// </summary>
        /// <returns>Count of warnings.</returns>
        /// <exception cref="HclSerializerException">Thrown if errors are returned by either of the CLI calls.</exception>
        private int FormatAndValidateOutput()
        {
            var validationOutput = new List<string>();

            this.settings.Logger.LogInformation("Formatting output...");
            this.settings.Runner.Run("fmt", true, true, msg => validationOutput.Add(msg));
            this.settings.Logger.LogInformation("Validating output...");
            this.settings.Runner.Run("validate", true, true, msg => validationOutput.Add(msg));
            this.Plan();

            // Don't bother trying to parse out warnings form text output. They're already displayed so just count.
            return validationOutput.Count(line => line.Contains("Warning:"));
        }

        /// <summary>
        /// Runs <c>terraform plan</c> to get a list of changes that the user should be warned about
        /// </summary>
        private void Plan()
        {
            var destructiveChanges = 0;
            var totalChanges = 0;

            this.settings.Logger.LogInformation("Planning...");
            this.settings.Runner.Run("plan", false, false, msg => LogChange(JObject.Parse(msg)), "-json");

            if (destructiveChanges > 0)
            {
                this.warnings.Add(
                    "One or more resources will be REPLACED or DESTROYED with the current configuration!");
            }

            if (totalChanges == 0)
            {
                this.settings.Logger.LogInformation(
                    "No changes were detected by 'terraform plan', however the configuration should still be tested against a non-prod stack.");
            }

            // ReSharper disable once SuggestBaseTypeForParameter - these are always JObject
            void LogChange(JObject planOutput)
            {
                var type = SafeGetToken("type");

                if (type == null)
                {
                    return;
                }

                switch (type)
                {
                    case "planned_change":

                        var addr = SafeGetToken("$.change.resource.addr");
                        var action = SafeGetToken("$.change.action");

                        ++totalChanges;

                        if (addr == null || action == null)
                        {
                            break;
                        }

                        switch (action)
                        {
                            case "replace":
                            case "delete":

                                var warn = $"Resource \"{addr}\" {PlanActions[action]} Run 'terraform plan' to see the reason.";
                                this.settings.Logger.LogWarning(warn);
                                this.warnings.Add(warn);
                                ++destructiveChanges;
                                break;

                            default:

                                this.settings.Logger.LogInformation($"Resource \"{addr}\" {PlanActions[action]}");
                                break;
                        }

                        break;

                    case "diagnostic":

                        var severity = SafeGetToken("$.diagnostic.severity");
                        var summary = SafeGetToken("$.diagnostic.summary");
                        var detail = SafeGetToken("$.diagnostic.detail").Replace("\n", string.Empty);
                        var msg = $"{summary}: {detail}";

                        if (severity == "error")
                        {
                            this.settings.Logger.LogError("ERROR: " + msg);
                            this.errors.Add(msg);
                        }
                        else
                        {
                            this.settings.Logger.LogWarning(msg);
                            this.warnings.Add(msg);
                        }

                        break;
                }

                string SafeGetToken(string jsonPath)
                {
                    var tok = planOutput.SelectToken(jsonPath);

                    return tok == null ? string.Empty : tok.Value<string>();
                }
            }
        }

        /// <summary>
        /// Resolves lambda code.
        /// For embedded (ZipFile) scripts, creates zipper resources for the files extracted by <see cref="LambdaFunctionImporter"/>.
        /// For S3 and container lambdas, sets the appropriate properties
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="stateFile">The state file.</param>
        private void ResolveLambdaCode(TextWriter writer, StateFile stateFile)
        {
            foreach (var cloudFormationResource in this.settings.Template.Resources.Where(
                         r => r.Type == TerraformExporterConstants.AwsLambdaFunction && r.GetResourcePropertyValue("Code") != null))
            {
                var mapping =
                    this.module.ResourceMappings.FirstOrDefault(ir => ir.LogicalId == cloudFormationResource.Name);

                if (mapping == null)
                {
                    continue;
                }

                var terraformResource = stateFile.FilteredResources(this.module.Name).FirstOrDefault(
                    rd => rd.Name == mapping.LogicalId && rd.Type == mapping.TerraformType);

                if (terraformResource == null)
                {
                    continue;
                }

                // Fix up state file.
                // ReSharper disable once AssignNullToNotNullAttribute - already asserted that this has a value in foreach statement.
                var lambdaCode =
                    ((Dictionary<object, object>)cloudFormationResource.GetResourcePropertyValue("Code")).ToDictionary(
                        kv => kv.Key.ToString(),
                        kv => kv.Value);
                var attributes = terraformResource.Instances.First().Attributes;

                if (lambdaCode.ContainsKey("ZipFile"))
                {
                    var runtimeObject = cloudFormationResource.GetResourcePropertyValue("Runtime");

                    if (runtimeObject == null)
                    {
                        // No runtime specified
                        continue;
                    }

                    ResolveLambdaZipCode(
                        writer,
                        runtimeObject.ToString(),
                        cloudFormationResource,
                        attributes,
                        mapping,
                        this.module.Inputs);
                }
                else if (lambdaCode.ContainsKey("ImageUri"))
                {
                    UpdatePropertyValue(
                        "image_uri",
                        cloudFormationResource.Template,
                        attributes,
                        mapping,
                        this.module.Inputs,
                        lambdaCode["ImageUri"]);
                }
                else
                {
                    ResolveLambdaS3Code(cloudFormationResource, attributes, mapping, this.module.Inputs, lambdaCode);
                }
            }
        }

        /// <summary>
        /// Resolves dependencies between resources and output values.
        /// </summary>
        /// <returns>List of output values to emit.</returns>
        private IEnumerable<OutputValue> ResolveOutputDependencies()
        {
            var outputValues = new List<OutputValue>();

            foreach (var output in this.settings.Template.Outputs)
            {
                if (!(output.Value is RefIntrinsic intrinsic))
                {
                    continue;
                }

                var resourceName = intrinsic.Reference;
                var evaluatedValue = intrinsic.Evaluate(this.settings.Template);
                var resource = this.module.ResourceMappings.FirstOrDefault(r => r.LogicalId == resourceName);

                if (resource == null)
                {
                    continue;
                }

                var terrafromAddress = resource.Address;

                var reference = evaluatedValue.ToString().StartsWith("arn:")
                                    ? $"{terrafromAddress}.arn"
                                    : $"{terrafromAddress}.id";

                outputValues.Add(
                    !string.IsNullOrEmpty(output.Description)
                        ? new OutputValue(output.Name, reference, output.Description)
                        : new OutputValue(output.Name, reference));
            }

            return outputValues;
        }

        /// <summary>
        /// Resolves dependencies within this module updating the in-memory copy of the state file
        /// and inputs to submodules with variable and resource references.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        private void ResolveDependencies(StateFile stateFile)
        {
            this.settings.Logger.LogInformation("\nResolving module's dependencies...");

            // Resources from state file that are declared by this module.
            var moduleResources = stateFile.FilteredResources(this.module.Name).ToList();

            var resolver = new ResourceDependencyResolver(
                this.settings,
                moduleResources,
                this.module,
                this.warnings);

            if (this.module.ResourceMappings.Any())
            {
                this.settings.Logger.LogInformation("- Resources...");
                foreach (var resourceMapping in this.module.ResourceMappings)
                {
                    resolver.ResolveResourceDependencies(
                        moduleResources.First(r => r.Name == resourceMapping.LogicalId));
                }
            }

            if (this.module.NestedModules.Any())
            {
                this.settings.Logger.LogInformation("- Submodules...");
                foreach (var importedModule in this.module.NestedModules)
                {
                    resolver.ResolveModuleDependencies(importedModule);
                }
            }
        }

        /// <summary>
        /// Writes the input variables and data blocks sections of <c>main.tf</c>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
        private void WriteInputsAndDataBlocks(TextWriter writer)
        {
            // TODO: Suppress any vars not referenced (e.g. only existed for condition blocks)
            foreach (var param in this.module.Inputs.OrderBy(p => p.IsDataSource).ThenBy(p => p.Name))
            {
                writer.WriteLine(param.GenerateHcl(true));
            }
        }

        /// <summary>
        /// Writes the locals section of <c>main.tf</c>
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
        private void WriteLocalsAndMappings(TextWriter writer)
        {
            // Output CF mappings as locals block
            new MappingSectionEmitter(writer, this.settings.Template.Mappings).Emit();
        }

        /// <summary>
        /// Serialize the in-memory copy of the state file and write out <c>main.tf</c>.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        /// <returns>Task to await.</returns>
        private async Task WriteMain(StateFile stateFile)
        {
            this.settings.Logger.LogInformation($"Writing {TerraformExporterConstants.MainScriptFile}");

            // Write main.tf
            using (var stream = new FileStream(
                       Path.Combine(this.settings.WorkspaceDirectory, this.settings.ModuleDirectory, TerraformExporterConstants.MainScriptFile),
                       FileMode.Create,
                       FileAccess.Write))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                this.ResolveLambdaCode(writer, stateFile);
                this.ResolveDependencies(stateFile);

                if (this.settings.IsRootModule)
                {
                    this.WriteProviders(writer);
                }

                await this.module.WriteModuleBlocksAsync();

                this.WriteInputsAndDataBlocks(writer);
                this.WriteLocalsAndMappings(writer);
                this.WriteResources(writer, stateFile);
                WriteOutputs(writer, this.ResolveOutputDependencies());
            }
        }

        /// <summary>
        /// Writes the terraform and  providers sections of <c>main.tf</c>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
        private void WriteProviders(TextWriter writer)
        {
            var builder = new ConfigurationBlockBuilder().WithRegion(this.settings.AwsRegion)
                .WithDefaultTag(this.settings.AddDefaultTag ? this.settings.StackName : null).WithZipper(
                    this.settings.Template.Resources.Any(
                        r => r.Type == TerraformExporterConstants.AwsLambdaFunction && r.GetResourcePropertyValue(TerraformExporterConstants.LambdaZipFile) != null));

            writer.Write(builder.Build());
        }

        /// <summary>
        /// Writes the resources section of <c>main.tf</c>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
        /// <param name="stateFile">The in-memory state file.</param>
        private void WriteResources(TextWriter writer, StateFile stateFile)
        {
            // Serialize the resources
            var serializer = new StateFileSerializer(new HclEmitter(writer));
            serializer.Serialize(stateFile, this.module.Name);
        }

        /// <summary>
        /// Write out <c>terraform.tfvars</c> using current values of parameters extracted from deployed CloudFormation stack.
        /// </summary>
        private void WriteTfVars()
        {
            if (!this.module.Inputs.Any())
            {
                return;
            }

            this.settings.Logger.LogInformation($"Writing {TerraformExporterConstants.VarsFile}");

            using (var stream = new FileStream(
                       Path.Combine(this.settings.WorkspaceDirectory, this.settings.ModuleDirectory, TerraformExporterConstants.VarsFile),
                       FileMode.Create,
                       FileAccess.Write))
            using (var s = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                var title = $"# Variable values as per current state of stack \"{this.settings.StackName}\"";
                var border = new string('#', title.Length);

                s.WriteLine(border);
                s.WriteLine("#");
                s.WriteLine(title);
                s.WriteLine("#");
                s.WriteLine(border);
                s.WriteLine();

                foreach (var input in this.module.Inputs.OrderBy(p => p.Name))
                {
                    var hcl = input.GenerateVariableAssignment();

                    if (!string.IsNullOrEmpty(hcl))
                    {
                        s.WriteLine(input.GenerateVariableAssignment());
                        s.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// Enumerate the resources and issue warnings for those that aren't fully imported..
        /// </summary>
        /// <param name="stateFile">In-memory state file</param>
        private void GenerateWarnings(StateFile stateFile)
        {
            var moduleResources = stateFile.FilteredResources(this.module.Name).ToList();

            // Scan for AWS::Cloudformation::Init metadata and warn about it.
            foreach (var templateResource in this.settings.Template.Resources.Where(
                r => r.Metadata != null && r.Metadata.Keys.Contains("AWS::CloudFormation::Init")))
            {
                this.warnings.Add(
                    $"Resource \"{GetQualifiedResourceName(templateResource)}\" contains AWS::CloudFormation::Init metadata which is not imported.");
            }

            // Scan for UserData and warn about it
            var userDataTypes = new[] { "AWS::AutoScaling::LaunchConfiguration", "AWS::EC2::Instance" };

            foreach (var templateResource in this.settings.Template.Resources.Where(
                r => userDataTypes.Contains(r.Type) && r.Properties != null && r.Properties.ContainsKey("UserData")))
            {
                this.warnings.Add($"Resource \"{GetQualifiedResourceName(templateResource)}\" contains user data which is not correctly imported. A lifecycle meta-argument has been emitted to prevent resource replacement.");
            }

            // Scan for lambdas with embedded code (ZipFile) and warn about it
            foreach (var templateResource in this.settings.Template.Resources.Where(
                r => r.Type == TerraformExporterConstants.AwsLambdaFunction && r.GetResourcePropertyValue(TerraformExporterConstants.LambdaZipFile) != null))
            {
                this.warnings.Add(
                    $"Resource \"{GetQualifiedResourceName(templateResource)}\" contains embedded function code (ZipFile) which may not be the latest version.");
            }

            // Scan for bucket polices and warn
            foreach (var templateResource in this.settings.Template.Resources.Where(r => r.Type == "AWS::S3::BucketPolicy"))
            {
                this.warnings.Add($"Resource \"{GetQualifiedResourceName(templateResource)}\" - Policy likely not imported. Add it in manually.");
            }

            string GetQualifiedResourceName(IResource cloudFormationResource)
            {
                var terraformResource = moduleResources.First(r => r.Name == cloudFormationResource.Name);

                return this.module.IsRootModule
                           ? terraformResource.Address
                           : $"module.{this.module.Name}.{terraformResource.Address}";
            }
        }
    }
}