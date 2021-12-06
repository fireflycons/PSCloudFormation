namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal partial class ResourceDependencyResolver
    {
        private readonly IReadOnlyCollection<CloudFormationResource> cloudFormationResources;

        private readonly IReadOnlyCollection<InputVariable> inputs;

        private readonly ITemplate template;

        private readonly IReadOnlyCollection<StateFileResourceDeclaration> terraformResources;

        private CloudFormationResource currentCloudFormationResource;

        private StateFileResourceDeclaration currentTerraformStateFileResource;

        private List<IntrinsicInfo> ReferenceLocations;

        public ResourceDependencyResolver(
            IReadOnlyCollection<CloudFormationResource> cloudFormationResources,
            IReadOnlyCollection<StateFileResourceDeclaration> terraformResources,
            IReadOnlyCollection<InputVariable> inputs)
        {
            this.cloudFormationResources = cloudFormationResources;
            this.template = cloudFormationResources.First().TemplateResource.Template;
            this.terraformResources = terraformResources;
            this.inputs = inputs;
        }

        public void ResolveDependencies(StateFileResourceDeclaration terraformStateFileResource)
        {
            // Get CF resource for the current state file resource entry
            this.currentCloudFormationResource =
                this.cloudFormationResources.First(r => r.LogicalResourceId == terraformStateFileResource.Name);
            this.currentTerraformStateFileResource = terraformStateFileResource;

            // Visit the CF resource gathering all intrinsics that might imply reference to another resource or input
            var intrinsicVisitor = new IntrinsicVisitor(
                this.cloudFormationResources,
                this.terraformResources,
                this.inputs);
            this.currentCloudFormationResource.TemplateResource.Accept(intrinsicVisitor);
            this.ReferenceLocations = intrinsicVisitor.ReferenceLocations;

            // Visit the terraform resource finding value matches between resource attributes and intrinsic evaluations, recording what needs to be modified
            var dependencyContext = new TerraformAttributeSetterContext(
                intrinsicVisitor.ReferenceLocations,
                this.template,
                false);
            terraformStateFileResource.StateFileResourceInstance.Attributes.Accept(
                new TerraformAttributeSetterVisitor(),
                dependencyContext);

            // For each found modification, update attribute value with JSON encoded reference expression.
            foreach (var mod in dependencyContext.Modifications.Where(m => m.NewReference != null))
            {
                if (mod.ContainingProperty == null)
                {
                    // Normal resource attribute
                    var token =
                        terraformStateFileResource.StateFileResourceInstance.Attributes.SelectToken(mod.Json.Path);
                    switch (token.Parent)
                    {
                        case JProperty jp:

                            jp.Value = mod.NewReference.ToJConstructor();
                            break;

                        case JArray ja:

                            ja[mod.Index] = mod.NewReference.ToJConstructor();
                            break;
                    }
                }
                else
                {
                    // Modification lies within nested JSON
                    // First, deserialize the nested JSON - which will work because we already did it one.
                    StateFileSerializer.TryGetJson(
                        mod.ContainingProperty.Value.Value<string>(),
                        false,
                        "Unknown",
                        "Unknown",
                        out JContainer document);

                    var token = document.SelectToken(mod.Json.Path);

                    switch (token.Parent)
                    {
                        case JProperty jp:

                            jp.Value = mod.NewReference.ToJConstructor();
                            break;

                        case JArray ja:

                            ja[mod.Index] = mod.NewReference.ToJConstructor();
                            break;
                    }

                    // Now put back the nested JSON complete with added reference
                    var enc = document.ToString(Formatting.None);
                    var val = new JValue(enc);
                    mod.ContainingProperty.Value = enc;
                }
            }
        }
    }
}