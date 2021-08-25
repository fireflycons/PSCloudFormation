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

    using Newtonsoft.Json;

    internal class TerraformExporter : AutoResourceLoader
    {
        [EmbeddedResource("terraform-resource-map.json")]

        // ReSharper disable once StyleCop.SA1600 - Loaded by auto-resource
        private static string resourceMapJson;

        private readonly ILogger logger;

        private readonly ITerraformSettings settings;

        private readonly IList<StackResource> stackResources;

        public TerraformExporter(IList<StackResource> stackResources, ITerraformSettings settings, ILogger logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.stackResources = stackResources;
        }

        public void Export()
        {
            var resourceMap = JsonConvert.DeserializeObject<List<ResourceMapping>>(resourceMapJson);
            var initialHcl = new StringBuilder();
            var finalHcl = new StringBuilder();
            var resourcesToImport = new List<ResourceImport>();
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
            initialHcl.AppendFormat("provider \"aws\" {{ region = \"{0}\" }}\n", this.settings.AwsRegion);
            finalHcl.AppendLine(TerraformBlock);
            finalHcl.AppendFormat("provider \"aws\" {{ region = \"{0}\" }}\n", this.settings.AwsRegion);

            this.logger.LogInformation("Processing stack resources and mapping to terraform resource types...");
            foreach (var resource in this.stackResources)
            {
                var mapping = resourceMap.FirstOrDefault(m => m.Aws == resource.ResourceType);

                if (mapping == null)
                {
                    this.logger.LogWarning(
                        $"Unable to map resource {resource.LogicalResourceId} ({resource.ResourceType})");
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

            if (!resourcesToImport.Any())
            {
                this.logger.LogWarning("No resources were found that could be imported.");
                return;
            }

            // Write out the HCL
            if (!Directory.Exists(this.settings.WorkspaceDirectory))
            {
                Directory.CreateDirectory(this.settings.WorkspaceDirectory);
            }

            var cwd = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(this.settings.WorkspaceDirectory);
                File.WriteAllText("main.tf", initialHcl.ToString(), new UTF8Encoding(false));

                this.logger.LogInformation("\nInitializing workspace...");
                this.settings.Runner.Run("init", true);
                this.logger.LogInformation("\nImporting mapped resources to terraform state...");

                foreach (var resource in resourcesToImport)
                {
                    if (this.settings.Runner.Run("import", false, resource.Address, resource.PhysicalId))
                    {
                        finalHcl.AppendLine(this.settings.Runner.GetResourceDefinition(resource.Address));
                    }
                    else
                    {
                        importFailures.Add($"{resource.Address} from {resource.AwsAddress}");
                    }
                }

                File.WriteAllText("main.tf", finalHcl.ToString(), new UTF8Encoding(false));

                // Now try to fix up the script by running terraform plan until we get no errors or the same errors repeating
                var passes = 1;
                var script = new HclScript("main.tf");
                var lastErrorHash = 0;

                this.logger.LogInformation("\nRunning 'terraform plan' and attempting to fix issues...");
                this.logger.LogInformation("- Pass 1");
                var errors = this.settings.Runner.RunPlan();
                var thisErrorHash = errors?.GetHashCode() ?? -1;

                while (lastErrorHash != thisErrorHash)
                {
                    if (errors == null)
                    {
                        this.logger.LogInformation("All HCL issues were successfully corrected!");
                        this.logger.LogInformation(string.Empty);
                        return;
                    }

                    // Do fixes
                    this.logger.LogInformation(
                        $"  - Errors fixed: {errors.Count(error => PlanFixer.Fix(script, error))}/{errors.Count()}");
                    script.Save();

                    // Re-run plan
                    this.logger.LogInformation($"- Pass {++passes}");
                    errors = this.settings.Runner.RunPlan();
                    lastErrorHash = thisErrorHash;
                    thisErrorHash = errors?.GetHashCode() ?? -1;
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
                    "You may well still need to make manual edits to the script, for instance to add inputs/outputs or dependencies between resources.");

                Directory.SetCurrentDirectory(cwd);
            }
        }

        [DebuggerDisplay("{Address}: {PhysicalId}")]
        private class ResourceImport
        {
            public string Address { get; set; }

            public string AwsAddress { get; set; }

            public string PhysicalId { get; set; }
        }

        [DebuggerDisplay("{Aws} -> {Terraform}")]
        private class ResourceMapping
        {
            [JsonProperty("AWS")]
            public string Aws { get; set; }

            [JsonProperty("TF")]
            public string Terraform { get; set; }
        }
    }
}