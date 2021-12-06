namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    using Firefly.CloudFormation;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.Terraform.DependencyResolver;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.State;

    /// <summary>
    /// Controls the complete serialization process of the state file to HCL.
    /// </summary>
    internal class HclWriter
    {
        /// <summary>
        /// Name of the main script file
        /// </summary>
        public const string MainScriptFile = "main.tf";

        /// <summary>
        /// Name of the variable values file
        /// </summary>
        public const string VarsFile = "terraform.tfvars";

        /// <summary>
        /// The terraform block
        /// </summary>
        [EmbeddedResource("terraform-block.hcl")]
        // ReSharper disable once StyleCop.SA1600 - Loaded by auto-resource
#pragma warning disable 649
        private static string terraformBlock;
#pragma warning restore 649

        /// <summary>
        /// The terraform block
        /// </summary>
        [EmbeddedResource("terraform-block-with-tag.hcl")]
        // ReSharper disable once StyleCop.SA1600 - Loaded by auto-resource
#pragma warning disable 649
        private static string terraformBlockWithTag;
#pragma warning restore 649

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The settings
        /// </summary>
        private readonly ITerraformSettings settings;

        /// <summary>
        /// Initializes static members of the <see cref="HclWriter"/> class.
        /// </summary>
        static HclWriter()
        {
            // Must load manually, as the embedded members are accessed by a static method.
            ResourceLoader.LoadResources(MethodBase.GetCurrentMethod().DeclaringType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HclWriter"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="logger">The logger.</param>
        public HclWriter(ITerraformSettings settings, ILogger logger)
        {
            this.logger = logger;
            this.settings = settings;
        }

        /// <summary>
        /// Gets the terraform block.
        /// </summary>
        /// <param name="region">The region.</param>
        /// <param name="stackName">Name of the stack. If <c>null</c>, a <c>default_tags</c> block is not included with the provider declaration.</param>
        /// <returns>Terraform block as HCL string</returns>
        public static string GetTerraformBlock(string region, string stackName)
        {
            if (stackName != null)
            {
                return terraformBlockWithTag.Replace("AWS::Region", region).Replace("AWS::StackName", stackName);
            }

            return terraformBlock.Replace("AWS::Region", region);
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
            IReadOnlyCollection<ImportedResource> importedResources,
            IReadOnlyCollection<InputVariable> parameters)
        {
            this.WriteMain(importedResources, parameters, stateFile);
            this.WriteTfVars(parameters);
            return this.FormatAndValidateOutput();
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

            // Emit an aws_availability_zones block for any GetAZs
            writer.WriteLine($"data \"aws_availability_zones\" \"available\" {{");
            writer.WriteLine($"  state = \"available\"");
            writer.WriteLine("}");
            writer.WriteLine();
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
            this.settings.Runner.Run("fmt", true, (msg) => validationOutput.Add(msg));
            this.logger.LogInformation("Validating output...");
            this.settings.Runner.Run("validate", true, (msg) => validationOutput.Add(msg));

            return validationOutput.Count(line => line.Contains("Warning:"));
        }

        /// <summary>
        /// Resolves dependencies between resources updating the in-memory copy of the state file with variable and resource references.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="importedResources">The imported resources.</param>
        private void ResolveResourceDependencies(
            StateFile stateFile,
            IReadOnlyCollection<InputVariable> parameters,
            IEnumerable<ImportedResource> importedResources)
        {
            this.logger.LogInformation("\nResolving dependencies between resources...");

            var resolver = new ResourceDependencyResolver(this.settings.Resources, stateFile.Resources, parameters);

            foreach (var tfr in importedResources)
            {
                resolver.ResolveDependencies(stateFile.Resources.First(r => r.Name == tfr.LogicalId));
            }
        }

        /// <summary>
        /// Resolves dependencies between resources and output values.
        /// </summary>
        /// <param name="importedResources">The imported resources.</param>
        /// <returns>List of output values to emit.</returns>
        private IEnumerable<OutputValue> ResolveOutputDependencies(
            IReadOnlyCollection<ImportedResource> importedResources)
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
            IReadOnlyCollection<ImportedResource> importedResources,
            IReadOnlyCollection<InputVariable> parameters,
            StateFile stateFile)
        {
            this.logger.LogInformation($"Writing {MainScriptFile}");
            this.ResolveResourceDependencies(stateFile, parameters, importedResources);

            // Write main.tf
            using (var stream = new FileStream(
                Path.Combine(this.settings.WorkspaceDirectory, MainScriptFile),
                FileMode.Create,
                FileAccess.Write))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
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
            writer.Write(
                this.settings.AddDefaultTag
                    ? GetTerraformBlock(this.settings.AwsRegion, this.settings.StackName)
                    : GetTerraformBlock(this.settings.AwsRegion, null));
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