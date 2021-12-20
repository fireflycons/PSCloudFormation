namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormation;
    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.PSCloudFormation.LambdaPackaging;
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
                                                                                 { "destroy", "will be DESTROYED!" },
                                                                                 {
                                                                                     "update",
                                                                                     "will be updated in-place."
                                                                                 },
                                                                                 { "replace", "will be REPLACED!" }
                                                                             };

        /// <summary>
        /// Name of the main script file
        /// </summary>
        public const string MainScriptFile = "main.tf";

        /// <summary>
        /// Name of the variable values file
        /// </summary>
        public const string VarsFile = "terraform.tfvars";

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The settings
        /// </summary>
        private readonly ITerraformSettings settings;

        /// <summary>
        /// The warning list
        /// </summary>
        private readonly IList<string> warnings;

        /// <summary>
        /// The error list
        /// </summary>
        private readonly IList<string> errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="HclWriter"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="warnings">Warning list</param>
        /// <param name="errors">Error list</param>
        public HclWriter(ITerraformSettings settings, ILogger logger, IList<string> warnings, IList<string> errors)
        {
            this.errors = errors;
            this.warnings = warnings;
            this.logger = logger;
            this.settings = settings;
        }

        /// <summary>
        /// Generate and write out all HCL.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        /// <param name="importedResources">The imported resources.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Count of validation warnings.</returns>
        public int Serialize(
            StateFile stateFile,
            IReadOnlyCollection<ResourceMapping> importedResources,
            IList<InputVariable> parameters)
        {
            this.WriteMain(importedResources, parameters, stateFile);
            this.WriteTfVars(parameters);
            return this.FormatAndValidateOutput();
        }

        /// <summary>
        /// Resolves lambda s3 code location.
        /// </summary>
        /// <param name="cloudFormationResource">The cloud formation resource.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="mapping">The mapping.</param>
        /// <param name="codeDefinition">The code definition.</param>
        private static void ResolveLambdaS3Code(
            ITemplateObject cloudFormationResource,
            JObject attributes,
            ResourceMapping mapping,
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
                codeDefinition["S3Bucket"]);
            UpdatePropertyValue(
                "s3_key",
                cloudFormationResource.Template,
                attributes,
                mapping,
                codeDefinition["S3Key"]);

            if (codeDefinition.ContainsKey("S3ObjectVersion"))
            {
                UpdatePropertyValue(
                    "s3_object_version",
                    cloudFormationResource.Template,
                    attributes,
                    mapping,
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
        private static void ResolveLambdaZipCode(
            TextWriter writer,
            string runtime,
            ITemplateObject cloudFormationResource,
            JObject attributes,
            ResourceMapping mapping)
        {
            var traits = LambdaTraits.FromRuntime(runtime);

            var dirName = Path.Combine("lambda", cloudFormationResource.Name);
            var fileName = Path.Combine(dirName, $"index{traits.ScriptFileExtension}");
            var zipName = Path.Combine(dirName, $"{cloudFormationResource.Name}_deployment_package.zip");

            // Create zipper resource
            var zipperResource = $"{cloudFormationResource.Name}_deployment_package";

            var sb = new StringBuilder();

            sb.AppendLine($"resource \"zipper_file\" \"{zipperResource}\" {{")
                .AppendLine($"  source = \"{fileName.Replace("\\", "/")}\"")
                .AppendLine($"  output_path = \"{zipName.Replace("\\", "/")}\"").AppendLine("}");

            writer.WriteLine(sb.ToString());

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
                    $"index.{m.Groups["handler"].Value}");
            }

            UpdatePropertyValue(
                "filename",
                cloudFormationResource.Template,
                attributes,
                mapping,
                new IndirectReference($"zipper_file.{zipperResource}.output_path"));
            UpdatePropertyValue(
                "source_code_hash",
                cloudFormationResource.Template,
                attributes,
                mapping,
                new IndirectReference($"zipper_file.{zipperResource}.output_sha"));
        }

        /// <summary>
        /// Updates a property value in the in-memory state file..
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="template">The template.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="resourceMapping">The resource mapping.</param>
        /// <param name="newValue">The new value.</param>
        private static void UpdatePropertyValue(
            string key,
            ITemplate template,
            JObject attributes,
            ResourceMapping resourceMapping,
            object newValue)
        {
            JToken newJToken;

            switch (newValue)
            {
                case IIntrinsic intrinsic:

                    newJToken = intrinsic.Render(template, resourceMapping).ToJConstructor();
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
        /// Writes the input variables and data blocks sections of <c>main.tf</c>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
        /// <param name="parameters">The parameters.</param>
        private static void WriteInputsAndDataBlocks(TextWriter writer, IEnumerable<InputVariable> parameters)
        {
            // TODO: Suppress any vars not referenced (e.g. only existed for condition blocks)
            foreach (var param in parameters.OrderBy(p => p.IsDataSource).ThenBy(p => p.Name))
            {
                writer.WriteLine(param.GenerateHcl(true));
            }
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
        /// Writes the resources section of <c>main.tf</c>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
        /// <param name="stateFile">The in-memory state file.</param>
        private static void WriteResources(TextWriter writer, StateFile stateFile)
        {
            // Serialize the resources
            var serializer = new StateFileSerializer(new HclEmitter(writer));
            serializer.Serialize(stateFile);
        }

        /// <summary>
        /// Formats and validates the generated output using <c>terraform fmt</c> and <c>terraform validate</c>.
        /// </summary>
        /// <returns>Count of warnings.</returns>
        /// <exception cref="HclSerializerException">Thrown if errors are returned by either of the CLI calls.</exception>
        private int FormatAndValidateOutput()
        {
            var validationOutput = new List<string>();

            this.logger.LogInformation("Formatting output...");
            this.settings.Runner.Run("fmt", true, true, msg => validationOutput.Add(msg));
            this.logger.LogInformation("Validating output...");
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

            this.logger.LogInformation("Planning...");
            var success = this.settings.Runner.Run(
                "plan",
                false,
                false,
                msg => LogChange(JObject.Parse(msg)),
                "-json");

            if (destructiveChanges > 0)
            {
                this.warnings.Add("One or more resources will be REPLACED or DESTROYED with the current configuration!");
            }

            if (totalChanges == 0)
            {
                this.logger.LogInformation("No changes were detected by 'terraform plan', however the configuration should still be tested against a non-prod stack.");
            }

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
                            case "destroy":

                                var warn = $"Resource \"{addr}\" {PlanActions[action]}";
                                this.logger.LogWarning(warn);
                                this.warnings.Add(warn);
                                ++destructiveChanges;
                                break;

                            default:

                                this.logger.LogInformation($"Resource \"{addr}\" {PlanActions[action]}");
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
                            this.logger.LogError("ERROR: " + msg);
                            this.errors.Add(msg);
                        }
                        else
                        {
                            this.logger.LogWarning(msg);
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
        /// <param name="importedResources">The imported resources.</param>
        private void ResolveLambdaCode(
            TextWriter writer,
            StateFile stateFile,
            IReadOnlyCollection<ResourceMapping> importedResources)
        {
            foreach (var cloudFormationResource in this.settings.Template.Resources.Where(
                r => r.Type == "AWS::Lambda::Function" && r.GetResourcePropertyValue("Code") != null))
            {
                var mapping = importedResources.FirstOrDefault(ir => ir.LogicalId == cloudFormationResource.Name);

                if (mapping == null)
                {
                    continue;
                }

                var terraformResource = stateFile.Resources.FirstOrDefault(
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

                    ResolveLambdaZipCode(writer, runtimeObject.ToString(), cloudFormationResource, attributes, mapping);
                }
                else if (lambdaCode.ContainsKey("ImageUri"))
                {
                    UpdatePropertyValue(
                        "image_uri",
                        cloudFormationResource.Template,
                        attributes,
                        mapping,
                        lambdaCode["ImageUri"]);
                }
                else
                {
                    ResolveLambdaS3Code(cloudFormationResource, attributes, mapping, lambdaCode);
                }
            }
        }

        /// <summary>
        /// Resolves dependencies between resources and output values.
        /// </summary>
        /// <param name="importedResources">The imported resources.</param>
        /// <returns>List of output values to emit.</returns>
        private IEnumerable<OutputValue> ResolveOutputDependencies(
            IReadOnlyCollection<ResourceMapping> importedResources)
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
                var resource = importedResources.FirstOrDefault(r => r.LogicalId == resourceName);

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
        /// Resolves dependencies between resources updating the in-memory copy of the state file with variable and resource references.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="importedResources">The imported resources.</param>
        private void ResolveResourceDependencies(
            StateFile stateFile,
            IList<InputVariable> parameters,
            IEnumerable<ResourceMapping> importedResources)
        {
            this.logger.LogInformation("\nResolving dependencies between resources...");

            var resolver = new ResourceDependencyResolver(
                this.settings.Resources,
                stateFile.Resources,
                parameters,
                this.warnings);

            foreach (var tfr in importedResources)
            {
                resolver.ResolveDependencies(stateFile.Resources.First(r => r.Name == tfr.LogicalId));
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
        /// <param name="importedResources">The imported resources.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="stateFile">The state file.</param>
        private void WriteMain(
            IReadOnlyCollection<ResourceMapping> importedResources,
            IList<InputVariable> parameters,
            StateFile stateFile)
        {
            this.logger.LogInformation($"Writing {MainScriptFile}");

            // Write main.tf
            using (var stream = new FileStream(
                Path.Combine(this.settings.WorkspaceDirectory, MainScriptFile),
                FileMode.Create,
                FileAccess.Write))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                this.ResolveLambdaCode(writer, stateFile, importedResources);
                this.ResolveResourceDependencies(stateFile, parameters, importedResources);
                this.WriteProviders(writer);
                WriteInputsAndDataBlocks(writer, parameters);
                this.WriteLocalsAndMappings(writer);
                WriteResources(writer, stateFile);
                WriteOutputs(writer, this.ResolveOutputDependencies(importedResources));
            }
        }

        /// <summary>
        /// Writes the terraform and  providers sections of <c>main.ff</c>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
        private void WriteProviders(TextWriter writer)
        {
            var builder = new ConfigurationBlockBuilder().WithRegion(this.settings.AwsRegion)
                .WithDefaultTag(this.settings.AddDefaultTag ? this.settings.StackName : null).WithZipper(
                    this.settings.Template.Resources.Any(
                        r => r.Type == "AWS::Lambda::Function" && r.GetResourcePropertyValue("Code.ZipFile") != null));

            writer.Write(builder.Build());
        }

        /// <summary>
        /// Write out <c>terraform.tfvars</c> using current values of parameters extracted from deployed CloudFormation stack.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        private void WriteTfVars(IEnumerable<InputVariable> parameters)
        {
            this.logger.LogInformation($"Writing {VarsFile}");

            using (var stream = new FileStream(
                Path.Combine(this.settings.WorkspaceDirectory, VarsFile),
                FileMode.Create,
                FileAccess.Write))
            using (var s = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                s.WriteLine("###################################################################");
                s.WriteLine("#");
                s.WriteLine($"# Variable values as per current state of stack \"{this.settings.StackName}\"");
                s.WriteLine("#");
                s.WriteLine("###################################################################");
                s.WriteLine();

                foreach (var param in parameters.OrderBy(p => p.Name))
                {
                    var hcl = param.GenerateTfVar();

                    if (!string.IsNullOrEmpty(hcl))
                    {
                        s.WriteLine(param.GenerateTfVar());
                        s.WriteLine();
                    }
                }
            }
        }
    }
}