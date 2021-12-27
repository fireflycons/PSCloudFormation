namespace Firefly.PSCloudFormation.Terraform
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Utils;

    using InvalidOperationException = Amazon.CloudFormation.Model.InvalidOperationException;

    /// <summary>
    /// Handles mapping of AWS -> Terraform resource types and generation of initial empty resource declarations for import.
    /// </summary>
    internal class ResourceMapper
    {
        /// <summary>
        /// The settings
        /// </summary>
        private readonly ITerraformExportSettings settings;

        /// <summary>
        /// The warning list
        /// </summary>
        private readonly IList<string> warnings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceMapper"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="warnings">The warnings.</param>
        public ResourceMapper(ITerraformExportSettings settings, IList<string> warnings)
        {
            this.settings = settings;
            this.warnings = warnings;
        }

        /// <summary>
        /// Processes the stack asynchronous.
        /// </summary>
        /// <returns>Module describing entire tree of nested stacks.</returns>
        public async Task<ModuleInfo> ProcessStackAsync()
        {
            if (this.settings.ExportNestedStacks)
            {
                return await ProcessChildModule(this.settings, this.warnings);
            }

            this.settings.Logger.LogInformation($"Processing stack {settings.StackName}...");

            using (var fs = AsyncFileHelpers.OpenAppendAsync(HclWriter.MainScriptFile))
            using (var writer = new StreamWriter(fs, AsyncFileHelpers.DefaultEncoding))
            {
                var module = new ModuleInfo(
                    this.settings,
                    null,
                    new InputVariableProcessor(this.settings, this.warnings).ProcessInputs(),
                    null);

                await module.ProcessResources(writer, this.warnings);

                return module;
            }
        }

        /// <summary>
        /// Recursively process nested stacks.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="warnings">The warnings.</param>
        /// <returns>Module being processed.</returns>
        private static async Task<ModuleInfo> ProcessChildModule(
            ITerraformExportSettings settings,
            IList<string> warnings)
        {
            var childModules = new List<ModuleInfo>();

            foreach (var child in settings.Resources.Where(
                         r => r.ResourceType == TerraformExporterConstants.AwsCloudFormationStack))
            {
                var logicalId = child.LogicalResourceId;
                var stackName = GetStackName(child.StackResource.PhysicalResourceId);
                var stackData = await StackHelper.ReadStackAsync(
                                    settings.CloudFormationClient,
                                    stackName,
                                    new Dictionary<string, object>());

                var childModuleSettings = settings.CopyWith(
                    stackData.Template,
                    stackData.Resources,
                    stackData.Outputs,
                    stackName,
                    Path.Combine("modules", stackName),
                    logicalId);

                childModules.Add(await ProcessChildModule(childModuleSettings, warnings));
            }

            var workingDirectory = Path.Combine(settings.WorkspaceDirectory, settings.ModuleDirectory);

            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }

            settings.Logger.LogInformation($"Processing stack {settings.StackName}...");
            var scriptFile = Path.Combine(workingDirectory, HclWriter.MainScriptFile);

            ModuleInfo module;

            using (var fs = settings.IsRootModule ? AsyncFileHelpers.OpenAppendAsync(scriptFile) : AsyncFileHelpers.OpenWriteAsync(scriptFile))
            using (var writer = new StreamWriter(fs, AsyncFileHelpers.DefaultEncoding))
            {
                var thisModuleSettings = settings.CopyWith(
                    settings.Template,
                    settings.Resources.Where(r => r.ResourceType != TerraformExporterConstants.AwsCloudFormationStack),
                    null,
                    settings.StackName,
                    settings.IsRootModule ? "." : settings.ModuleDirectory,
                    settings.LogicalId);

                module = new ModuleInfo(
                    thisModuleSettings,
                    childModules,
                    new InputVariableProcessor(thisModuleSettings, warnings).ProcessInputs(),
                    thisModuleSettings.CloudFormationOutputs);

                await module.ProcessResources(writer, warnings);
            }

            await module.WriteModuleBlocksAsync();

            return module;
        }

        /// <summary>
        /// Gets the name of the stack from its ARN.
        /// </summary>
        /// <param name="stackId">The stack identifier.</param>
        /// <returns>The stack name.</returns>
        /// <exception cref="System.InvalidOperationException">'{stackId}' is not a valid stack ARN</exception>
        private static string GetStackName(string stackId)
        {
            if (!stackId.StartsWith("arn:"))
            {
                return stackId;
            }

            var match = StackHelper.StackIdRegex.Match(stackId);

            if (match.Success)
            {
                return match.Groups["stack"].Value;
            }

            throw new InvalidOperationException($"'{stackId}' is not a valid stack ARN");
        }
    }
}