namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.TemplateObjects.Traversal.AcceptExtensions;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Fixes up dependencies between resources and other resources/inputs/data sources
    /// </summary>
    internal partial class ResourceDependencyResolver
    {
        /// <summary>
        /// All exported CloudFormation resources.
        /// </summary>
        private readonly IReadOnlyCollection<CloudFormationResource> cloudFormationResources;

        /// <summary>
        /// All input variables generated from the exported CloudFormation Stack.
        /// </summary>
        private readonly IReadOnlyCollection<InputVariable> inputs;

        /// <summary>
        /// Reference to the parsed CloudFormation template
        /// </summary>
        private readonly ITemplate template;

        /// <summary>
        /// All imported terraform resources (as JSON from state file).
        /// </summary>
        private readonly IReadOnlyCollection<StateFileResourceDeclaration> terraformResources;

        /// <summary>
        /// The CloudFormation resource currently being processed.
        /// </summary>
        private CloudFormationResource currentCloudFormationResource;

        /// <summary>
        /// The warning list
        /// </summary>
        private readonly IList<string> warnings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDependencyResolver"/> class.
        /// </summary>
        /// <param name="cloudFormationResources">All exported CloudFormation resources..</param>
        /// <param name="terraformResources">All imported terraform resources (as JSON from state file).</param>
        /// <param name="inputs">All input variables generated from the exported CloudFormation Stack.</param>
        /// <param name="warnings"></param>
        public ResourceDependencyResolver(
            IReadOnlyCollection<CloudFormationResource> cloudFormationResources,
            IReadOnlyCollection<StateFileResourceDeclaration> terraformResources,
            IReadOnlyCollection<InputVariable> inputs,
            IList<string> warnings)
        {
            this.warnings = warnings;
            this.cloudFormationResources = cloudFormationResources;
            this.template = cloudFormationResources.First().TemplateResource.Template;
            this.terraformResources = terraformResources;
            this.inputs = inputs;
        }

        /// <summary>
        /// Resolves the dependencies for the given terraform resource.
        /// </summary>
        /// <param name="terraformStateFileResource">The current terraform resource from state file.</param>
        public void ResolveDependencies(StateFileResourceDeclaration terraformStateFileResource)
        {
            try
            {
                var referenceLocations = new List<IntrinsicInfo>();

                // Get CF resource for the current state file resource entry
                this.currentCloudFormationResource =
                    this.cloudFormationResources.First(r => r.LogicalResourceId == terraformStateFileResource.Name);

                // Find related resources that may be merged into this one
                var relatedResources = this.template.DependencyGraph.Edges.Where(
                    e => e.Source.Name == this.currentCloudFormationResource.LogicalResourceId && e.Target.TemplateObject is IResource res && TerraformExporter.MergedResources.Contains(res.Type))
                    .Select(e => (IResource)e.Target.TemplateObject);

                foreach (var cloudFormationResource in new[] { this.currentCloudFormationResource.TemplateResource }
                    .Concat(relatedResources))
                {
                    // Visit the CF resource gathering all intrinsics that might imply reference to another resource or input
                    var intrinsicVisitorContext = new IntrinsicVisitorContext(
                        this.cloudFormationResources,
                        this.terraformResources,
                        this.inputs,
                        cloudFormationResource,
                        this.warnings);

                    var intrinsicVisitor =
                        new IntrinsicVisitor(this.template);
                    cloudFormationResource.Accept(intrinsicVisitor, intrinsicVisitorContext);

                    referenceLocations.AddRange(intrinsicVisitorContext.ReferenceLocations);
                }

                // Visit the terraform resource finding value matches between resource attributes and intrinsic evaluations, recording what needs to be modified
                var dependencyContext = new TerraformAttributeSetterContext(
                    referenceLocations,
                    this.template,
                    terraformStateFileResource);

                terraformStateFileResource.StateFileResourceInstance.Attributes.Accept(
                    new TerraformAttributeSetterVisitor(),
                    dependencyContext);

                // For each found modification, update attribute value with JSON encoded reference expression or string interpolation.
                foreach (var modification in dependencyContext.Modifications.Where(
                    m => m.Reference != null))
                {
                    if (modification.ContainingProperty == null)
                    {
                        // Normal resource attribute
                        ApplyNewValue(
                            terraformStateFileResource.StateFileResourceInstance.Attributes.SelectToken(
                                modification.ValueToReplace.Path),
                            modification);
                    }
                    else
                    {
                        // Modification lies within nested JSON, e.g. a policy document.
                        // First, deserialize the nested JSON - which will work because we already did it once.
                        StateFileSerializer.TryGetJson(
                            modification.ContainingProperty.Value.Value<string>(),
                            false,
                            terraformStateFileResource.Name,
                            terraformStateFileResource.Type,
                            out var document);

                        ApplyNewValue(document.SelectToken(modification.ValueToReplace.Path), modification);

                        // Now put back the nested JSON complete with added reference
                        var enc = document.ToString(Formatting.None);
                        modification.ContainingProperty.Value = enc;
                    }
                }

            }
            catch (HclSerializerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new HclSerializerException(
                    $"Internal error: {e.Message}",
                    terraformStateFileResource.Name,
                    terraformStateFileResource.Type,
                    e);
            }
        }

        /// <summary>
        /// Applies the new reference or interpolated value to the given JToken.
        /// </summary>
        /// <param name="token">The token to replace.</param>
        /// <param name="modification">The modification data.</param>
        private static void ApplyNewValue(JToken token, StateModification modification)
        {
            var newValue = modification.Reference.ToJConstructor();

            switch (token.Parent)
            {
                case JProperty jp:

                    jp.Value = newValue;
                    break;

                case JArray ja:

                    ja[modification.Index] = newValue;
                    break;
            }
        }
    }
}