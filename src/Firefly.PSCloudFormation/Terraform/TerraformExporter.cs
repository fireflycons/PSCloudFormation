namespace Firefly.PSCloudFormation.Terraform
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormation;
    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.CloudFormationParser.TemplateObjects;
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

        /// <summary>
        /// These resources have no direct terraform representation.
        /// They are merged into the resources that depend on them
        /// when the dependent resource is imported.
        /// </summary>
        private static readonly List<string> MergedResources =
            new List<string> { "AWS::CloudFront::CloudFrontOriginAccessIdentity", "AWS::IAM::Policy", "AWS::EC2::SecurityGroupIngress", "AWS::EC2::SecurityGroupEgress", "AWS::EC2::VPCGatewayAttachment" };

        /// <summary>
        /// These resources are currently not supported for import.
        /// </summary>
        private static readonly List<string> UnsupportedResources = new List<string> { "AWS::ApiGateway::Deployment" };

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
            var unmappedResources = new List<string>();

            this.logger.LogInformation("\nInitializing inputs and resources...");

            initialHcl.AppendLine(terraformBlock.Replace("#REGION#", this.settings.AwsRegion));
            finalHcl.AppendLine(terraformBlock.Replace("#REGION#", this.settings.AwsRegion));

            var parameters = this.ProcessInputVariables();

            foreach (var hclParameter in parameters)
            {
                finalHcl.AppendLine(hclParameter.GenerateHcl(true));
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
            var terraformExecutionErrorCount = 0;
            var warningCount = 0;

            try
            {
                var totalResources = resourcesToImport.Count;
                var imported = 0;

                Directory.SetCurrentDirectory(this.settings.WorkspaceDirectory);

                // Write out initial HCL with empty resource declarations, so that terraform import has something to work with
                File.WriteAllText(MainScriptFile, initialHcl.ToString(), new UTF8Encoding(false));

                this.logger.LogInformation("\nInitializing workspace...");
                this.settings.Runner.Run("init", true, null);
                this.logger.LogInformation($"\nImporting {totalResources} mapped resources from stack \"{this.settings.StackName}\" to terraform state...");

                //importedResources = resourcesToImport;

                foreach (var resource in resourcesToImport)
                {
                    var resourceToImport = resource.PhysicalId;

                    this.logger.LogInformation(
                        $"\nImporting resource {++imported}/{totalResources} - {resource.Address}");

                    // PeeringRoute (AWS::EC2::Route): unexpected format of ID ("fc-ba-Peeri-VOSMB8EXFM2J"), expected ROUTETABLEID_DESTINATION
                    // PrivateRouteTableAssociationA (AWS::EC2::SubnetRouteTableAssociation): Unexpected format for import: rtbassoc-0f37b1f143000066c. Use 'subnet ID/route table ID' or 'gateway ID/route table ID
                    if (ResourceImporter.RequiresResoureImporter(resource.TerraformType))
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
                            resourceToImport = importer.GetImportId(
                                $"Select related resource for {resource.AwsAddress}",
                                null);

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

                // Now start adjusting the state file in accordance with the template's directed edge graph.
                this.logger.LogInformation("\nResolving dependencies between resources...");

                // One copy of the state file that we will modify and write back out (adding dependencies and sensitive attributes)
                var stateFile = JsonConvert.DeserializeObject<StateFile>(File.ReadAllText(StateFileName));

                // TODO: Analyze state file for null properties that have defaults. Replace these defaults and write back out.

                // A second copy of the state file that we will insert references to inputs, other resources etc. before serialization to HCL.
                var generatationStateFile = JsonConvert.DeserializeObject<StateFile>(File.ReadAllText(StateFileName));

                // Wire up dependencies
                this.ResolveResourceDependencies(importedResources, generatationStateFile);
                this.ResolveInputDependencies(importedResources, generatationStateFile, parameters);
                var outputs = this.ResolveOutputDependencies(importedResources, generatationStateFile);

                // Serialize to HCL
                this.logger.LogInformation("Writing main.tf");

                using (var stream = new FileStream(
                    Path.Combine(this.settings.WorkspaceDirectory, "main.tf"),
                    FileMode.Create,
                    FileAccess.Write))
                using (var s = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    s.Write(terraformBlock.Replace("#REGION#", this.settings.AwsRegion));

                    // TODO: Suppress any vars not referenced (e.g. only existed for condition blocks)
                    foreach (var param in parameters.OrderBy(p => p.IsDataSource).ThenBy(p => p.Name))
                    {
                        s.WriteLine(param.GenerateHcl(false));
                    }

                    var serializer = new Serializer(new HclEmitter(s));
                    serializer.Serialize(generatationStateFile);

                    foreach (var output in outputs)
                    {
                        s.WriteLine(output.GenerateHcl());
                    }
                }

                var validationOutput = new List<string>();

                try
                {
                    this.logger.LogInformation("Formatting output...");
                    this.settings.Runner.Run("fmt", true, (msg) => validationOutput.Add(msg));
                    this.logger.LogInformation("Validating output...");
                    this.settings.Runner.Run("validate", true, (msg) => validationOutput.Add(msg));
                }
                catch
                {
                    terraformExecutionErrorCount += validationOutput.Count(line => line.Contains("Error:"));
                }

                warningCount += validationOutput.Count(line => line.Contains("Warning:"));

                if (terraformExecutionErrorCount + this.errors.Count > 0)
                {
                    throw new HclSerializerException($"Stack \"{this.settings.StackName}\": Errors were detected!");
                }

                this.logger.LogInformation($"Export of stack \"{this.settings.StackName}\" to terraform complete!");
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
                this.logger.LogInformation("\n");

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
                    r => userDataTypes.Contains(r.Type) && r.Properties != null
                                                        && r.Properties.ContainsKey("UserData")))
                {
                    this.warnings.Add(
                        $"Resource \"{templateResource.Name}\" contains user data which is not correctly imported.");
                }

                // Scan for lambdas with embedded code (ZipFile) and warn about it
                foreach (var templateResource in this.settings.Template.Resources.Where(
                    r => r.Type == "AWS::Lambda::Function" && r.GetResourcePropertyValue("Code.ZipFile") != null))
                {
                    this.warnings.Add(
                        $"Resource \"{templateResource.Name}\" contains embedded function code (ZipFile) which is not imported.");
                }

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
                    this.logger.LogInformation("\nIt appears this stack contains custom resources. For a suggestion on how to manage these with terraform, see");
                    this.logger.LogInformation("https://trackit.io/trackit-whitepapers/cloudformation-to-terraform-conversion/");
                }

                this.logger.LogInformation($"\n       Errors: {totalErrors}, Warnings: {warningCount + this.warnings.Count}\n");
                Directory.SetCurrentDirectory(cwd);
            }
        }

        private static void ReplacePolicyReference(
            JToken node,
            IReferencedItem id,
            JConstructor replacement,
            ref bool replacementMade)
        {
            switch (node.Type)
            {
                case JTokenType.Object:

                    foreach (var child in node.Children<JProperty>())
                    {
                        if (child.Value is JValue jv && jv.Value is string s
                                                     && (s == id.ScalarIdentity || id.ArnRegex.IsMatch(s)))
                        {
                            // Smuggle in an encoded type:constructor_parameter for a "Reference" derivative
                            // See Scalar constructor.
                            jv.Value = replacement.Name;
                            replacementMade = true;
                        }
                        else
                        {
                            ReplacePolicyReference(child.Value, id, replacement, ref replacementMade);
                        }
                    }

                    // do something?
                    break;

                case JTokenType.Array:

                    foreach (var child in node.Children())
                    {
                        ReplacePolicyReference(child, id, replacement, ref replacementMade);
                    }

                    break;

                case JTokenType.String:

                    var jv1 = (JValue)node;
                    var s1 = (string)jv1.Value;

                    if (s1 == id.ScalarIdentity || id.ArnRegex.IsMatch(s1))
                    {
                        // Smuggle in an encoded type:constructor_parameter for a "Reference" derivative
                        // See Scalar constructor.
                        ((JValue)node).Value = replacement.Name;
                        replacementMade = true;
                    }

                    break;
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
        /// <param name="unmappedResources">(OUT) AWS resources that were not successfully mapped.</param>
        /// <returns>A list of <see cref="ResourceImport"/> for successfully mapped AWS resources.</returns>
        private List<ResourceImport> ProcessResources(StringBuilder initialHcl, ICollection<string> unmappedResources)
        {
            var resourceMap = JsonConvert.DeserializeObject<List<ResourceMapping>>(resourceMapJson);
            var resourcesToImport = new List<ResourceImport>();
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

        private List<OutputValue> ResolveOutputDependencies(
            List<ResourceImport> importedResources,
            StateFile generatationStateFile)
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
        /// Resolves dependencies between resources and parameters.
        /// </summary>
        /// <param name="importedResources">The imported resources.</param>
        /// <param name="hclGenerationStateFile">The HCL generation state file.</param>
        /// <param name="inputVariables">The input variables.</param>
        private void ResolveInputDependencies(
            IReadOnlyCollection<ResourceImport> importedResources,
            StateFile hclGenerationStateFile,
            IReadOnlyCollection<InputVariable> inputVariables)
        {
            var edges = this.settings.Template.DependencyGraph.Edges.Where(
                e => e.Source is IParameterVertex && e.Target is ResourceVertex);

            foreach (var edge in this.settings.Template.DependencyGraph.Edges.Where(
                e => e.Source is IParameterVertex && e.Target is ResourceVertex))
            {
                var referringAwsResource = this.settings.Resources.First(r => r.LogicalResourceId == edge.Target.Name);

                var referringTerraformResource =
                    importedResources.FirstOrDefault(r => referringAwsResource.LogicalResourceId == r.LogicalId);

                if (referringTerraformResource == null)
                {
                    // Resource wasn't imported
                    continue;
                }

                var referredParameter = inputVariables.FirstOrDefault(p => p.Name == edge.Source.Name);

                if (referredParameter == null)
                {
                    if (edge.Source.Name.StartsWith("AWS::"))
                    {
                        this.warnings.Add($"Resource \"{referringAwsResource.LogicalResourceId}\" references \"{edge.Source.Name}\" which is not supported by terraform.");
                    }
                    else
                    {
                        var msg = $"Cannot find input variable with name \"{edge.Source.Name}\"";

                        if (!this.warnings.Contains(msg))
                        {
                            this.warnings.Add(msg);
                        }
                    }

                    continue;
                }

                var referringState = hclGenerationStateFile.Resources
                    .First(r => r.Address == referringTerraformResource.Address).Instances.First();

                if (edge.Tag?.ReferenceType != ReferenceType.ParameterReference)
                {
                    continue;
                }

                Reference newValue;

                if (referredParameter.IsDataSource)
                {
                    newValue = new DataSourceReference(referredParameter.Address);
                }
                else
                {
                    newValue = new ParameterReference(referredParameter.Address);
                }

                var con = newValue.ToJConstructor();
                var resourceAttributes = referringState.Attributes.ToString();

                foreach (var node in referringState.FindId(referredParameter))
                {
                    // TODO: deal with list() parameter values
                    switch (node)
                    {
                        case JProperty jp when jp.Value is JValue jv:

                            if (Serializer.TryGetJson(jv.Value<string>(), true, out var policy))
                            {
                                var replacementMade = false;
                                ReplacePolicyReference(policy, referredParameter, con, ref replacementMade);

                                if (replacementMade)
                                {
                                    jv.Value = policy.ToString(Formatting.None);
                                }
                            }
                            else
                            {
                                if (referredParameter.IsScalar)
                                {
                                    var stringValue = jv.Value<string>();

                                    if (stringValue != null)
                                    {
                                        if (stringValue == referredParameter.ScalarIdentity)
                                        {
                                            jp.Value = con;
                                        }
                                        else if (stringValue.Contains(referredParameter.ScalarIdentity))
                                        {
                                            jp.Value = new JValue(
                                                stringValue.Replace(
                                                    referredParameter.CurrentValue.ToString(),
                                                    $"${{{referredParameter.Address}}}"));
                                        }
                                    }
                                    else
                                    {
                                        jp.Value = con;
                                    }
                                }
                                else
                                {
                                    // Where the input var is a list, the individual value is replaced with an indexed reference. 
                                    var ind = referredParameter.IndexOf(jv.Value<string>());

                                    if (ind == -1)
                                    {
                                        continue;
                                    }

                                    newValue = new ParameterReference(referredParameter.Address, ind);
                                    jp.Value = newValue.ToJConstructor();
                                }
                            }

                            break;

                        case JArray ja:

                            if (referredParameter.IsScalar)
                            {
                                for (var ind = 0; ind < ja.Count; ++ind)
                                {
                                    if (ja[ind] is JConstructor)
                                    {
                                        // this one's already been set to something else
                                        continue;
                                    }

                                    var stringVal = ja[ind].Value<string>();

                                    if (stringVal == referredParameter.CurrentValue.ToString())
                                    {
                                        ja.RemoveAt(ind);
                                        ja.Add(con);
                                    }
                                }
                            }
                            else
                            {
                                // Where the input var is a list and all its values match all the values in this JArray
                                // then the JArray is replaced with the reference
                                if (referredParameter.ListIdentity.OrderBy(s => s)
                                    .SequenceEqual(ja.Values<string>().OrderBy(s => s)))
                                {
                                    if (ja.Parent is JProperty jp)
                                    {
                                        jp.Value = con;
                                    }
                                }
                                else
                                {
                                    // When there isn't a complete match on all values, then the individual value is replaced with and indexed reference.
                                    for (var jarrayIndex = 0; jarrayIndex < ja.Count; ++jarrayIndex)
                                    {
                                        if (!(ja[jarrayIndex] is JValue jv) || !(jv.Value is string s))
                                        {
                                            continue;
                                        }

                                        var paramIndex = referredParameter.IndexOf(s);

                                        if (paramIndex != -1)
                                        {
                                            newValue = new ParameterReference(referredParameter.Address, paramIndex);
                                            ja[jarrayIndex] = newValue.ToJConstructor();
                                        }
                                    }
                                }
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Resolves dependencies between resources.
        /// </summary>
        /// <param name="importedResources">The imported resources.</param>
        /// <param name="hclGenerationStateFile">The HCL generation state file.</param>
        private void ResolveResourceDependencies(
            IReadOnlyCollection<ResourceImport> importedResources,
            StateFile hclGenerationStateFile)
        {
            foreach (var edge in this.settings.Template.DependencyGraph.Edges.Where(
                e => e.Source is ResourceVertex && e.Target is ResourceVertex))
            {
                var referredAwsResource = this.settings.Resources.First(r => r.LogicalResourceId == edge.Source.Name);
                var referringAwsResource = this.settings.Resources.First(r => r.LogicalResourceId == edge.Target.Name);

                var referredTerraformResource =
                    importedResources.FirstOrDefault(r => referredAwsResource.LogicalResourceId == r.LogicalId);
                var referringTerraformResource =
                    importedResources.FirstOrDefault(r => referringAwsResource.LogicalResourceId == r.LogicalId);

                if (referredTerraformResource == null || referringTerraformResource == null)
                {
                    // Resource wasn't imported
                    continue;
                }

                var referringState = hclGenerationStateFile.Resources
                    .First(r => r.Address == referringTerraformResource.Address).Instances.First();

                if (edge.Tag == null)
                {
                    continue;
                }

                switch (edge.Tag.ReferenceType)
                {
                    case ReferenceType.DirectReference:

                        {
                            var reference = new DirectReference(referredTerraformResource.Address);
                            var con = reference.ToJConstructor();

                            UpdateState(referringState, referredAwsResource, con);
                            break;
                        }

                    case ReferenceType.AttributeReference:

                        {
                            // Referring state needs the attribute name of the referred state where the values match
                            // Not all attribute names may have 1:1 relationship, so a mapping might be needed.
                            var attribute = edge.Tag.AttributeName.CamelCaseToSnakeCase();

                            var referredState = hclGenerationStateFile.Resources
                                .First(r => r.Address == referredTerraformResource.Address).Instances.First();

                            // Get named attribute(s) in referred state
                            foreach (var prop in referredState.Attributes.Descendants()
                                .Where(t => t is JProperty jp && jp.Name == attribute))
                            {
                                var reference = new IndirectReference($"{referredState.Parent.Address}.{attribute}");
                                var con = reference.ToJConstructor();

                                UpdateState(referringState, referredAwsResource, con);
                            }

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Updates the state data with the new reference.
        /// </summary>
        /// <param name="referringState">State of the referring.</param>
        /// <param name="referredAwsResource">The referred AWS resource.</param>
        /// <param name="con">The <c>JConstructor</c> object to replace the constant value in the state.</param>
        private static void UpdateState(
            ResourceInstance referringState,
            CloudFormationResource referredAwsResource,
            JConstructor con)
        {
            foreach (var node in referringState.FindId(referredAwsResource))
            {
                switch (node)
                {
                    case JProperty jp when jp.Value is JValue jv:

                        if (Serializer.TryGetJson(jv.Value<string>(), true, out var policy))
                        {
                            var replacementMade = false;
                            ReplacePolicyReference(policy, referredAwsResource, con, ref replacementMade);

                            if (replacementMade)
                            {
                                jv.Value = policy.ToString(Formatting.None);
                            }
                        }
                        else
                        {
                            jp.Value = con;
                        }

                        break;

                    case JArray ja:

                        for (var ind = 0; ind < ja.Count; ++ind)
                        {
                            var stringVal = ja[ind].Value<string>();

                            if (stringVal == referredAwsResource.PhysicalResourceId)
                            {
                                ja.RemoveAt(ind);
                                ja.Add(con);
                            }
                        }

                        break;
                }
            }
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
    }
}