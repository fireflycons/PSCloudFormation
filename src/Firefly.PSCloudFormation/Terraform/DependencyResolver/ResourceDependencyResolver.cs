namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.CloudFormationParser.TemplateObjects.Traversal.AcceptExtensions;
    using Firefly.PSCloudFormation.Terraform.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Fixes up dependencies between resources and other resources/inputs/data sources
    /// </summary>
    internal partial class ResourceDependencyResolver
    {
        /// <summary>
        /// The module being processed
        /// </summary>
        private readonly ModuleInfo module;

        /// <summary>
        /// The settings
        /// </summary>
        private readonly ITerraformExportSettings settings;

        /// <summary>
        /// Reference to the parsed CloudFormation template
        /// </summary>
        private readonly ITemplate template;

        /// <summary>
        /// All imported terraform resources (as JSON from state file).
        /// </summary>
        private readonly IReadOnlyCollection<StateFileResourceDeclaration> terraformResources;

        /// <summary>
        /// The warning list
        /// </summary>
        private readonly IList<string> warnings;

        /// <summary>
        /// The CloudFormation resource currently being processed.
        /// </summary>
        private CloudFormationResource currentCloudFormationResource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDependencyResolver"/> class.
        /// </summary>
        /// <param name="settings">Main settings object.</param>
        /// <param name="terraformResources">All imported terraform resources (as JSON from state file).</param>
        /// <param name="module">The module being processed.</param>
        /// <param name="warnings">Global warning list.</param>
        public ResourceDependencyResolver(
            ITerraformExportSettings settings,
            IReadOnlyCollection<StateFileResourceDeclaration> terraformResources,
            ModuleInfo module,
            IList<string> warnings)
        {
            this.module = module;
            this.settings = settings;
            this.warnings = warnings;
            this.template = settings.Template;
            this.terraformResources = terraformResources;
        }

        /// <summary>
        /// Resolves the input dependencies of a module imported by the current module.
        /// </summary>
        /// <param name="referencedModule">The referenced module.</param>
        public void ResolveModuleDependencies(ModuleInfo referencedModule)
        {
            // Visit the AWS::CloudFormation::Stack resource gathering all intrinsics that might imply reference to another resource or input
            var intrinsicVisitorContext = new IntrinsicVisitorContext(
                this.settings,
                this.terraformResources,
                this.module.Inputs,
                referencedModule.StackResource,
                this.warnings,
                this.module);

            // Visit the AWS::CloudFormation::Stack associated with the given module and pull out
            // intrinsic references in Parameters property.
            var intrinsicVisitor = new IntrinsicVisitor(this.template);
            referencedModule.StackResource.Accept(intrinsicVisitor, intrinsicVisitorContext);

            foreach (var intrinsicInfo in intrinsicVisitorContext.ReferenceLocations)
            {
                var reference = this.GetModuleInputReference(referencedModule, intrinsicInfo);

                if (reference != null)
                {
                    ApplyModuleInputReference(referencedModule, intrinsicInfo, reference);
                }
            }
        }

        /// <summary>
        /// Resolves the dependencies for the given terraform resource.
        /// </summary>
        /// <param name="terraformStateFileResource">The current terraform resource from state file.</param>
        public void ResolveResourceDependencies(StateFileResourceDeclaration terraformStateFileResource)
        {
            try
            {
                var referenceLocations = new List<IntrinsicInfo>();

                // Get CF resource for the current state file resource entry
                this.currentCloudFormationResource =
                    this.settings.Resources.First(r => r.LogicalResourceId == terraformStateFileResource.Name);

                // Find related resources that may be merged into this one
                var relatedResources = this.template.DependencyGraph.Edges.Where(
                        e => e.Source.Name == this.currentCloudFormationResource.LogicalResourceId
                             && e.Target.TemplateObject is IResource res
                             && ModuleInfo.MergedResources.Contains(res.Type))
                    .Select(e => (IResource)e.Target.TemplateObject);

                foreach (var cloudFormationResource in new[] { this.currentCloudFormationResource.TemplateResource }
                             .Concat(relatedResources))
                {
                    // Visit the CF resource gathering all intrinsics that might imply reference to another resource or input
                    var intrinsicVisitorContext = new IntrinsicVisitorContext(
                        this.settings,
                        this.terraformResources,
                        this.module.Inputs,
                        cloudFormationResource,
                        this.warnings,
                        this.module);

                    var intrinsicVisitor = new IntrinsicVisitor(this.template);
                    cloudFormationResource.Accept(intrinsicVisitor, intrinsicVisitorContext);

                    referenceLocations.AddRange(intrinsicVisitorContext.ReferenceLocations);
                }

                // Visit the terraform resource finding value matches between resource attributes and intrinsic evaluations, recording what needs to be modified
                var dependencyContext = new TerraformAttributeSetterContext(
                    referenceLocations,
                    this.template,
                    terraformStateFileResource,
                    this.module.Inputs);

                terraformStateFileResource.ResourceInstance.Attributes.Accept(
                    new TerraformAttributeSetterVisitor(),
                    dependencyContext);

                // For each found modification, update attribute value with JSON encoded reference expression or string interpolation.
                foreach (var modification in dependencyContext.Modifications.Where(m => m.Reference != null))
                {
                    if (modification.ContainingProperty == null)
                    {
                        // Normal resource attribute
                        ApplyResourceAttributeReference(
                            terraformStateFileResource.ResourceInstance.Attributes.SelectToken(
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

                        ApplyResourceAttributeReference(
                            document.SelectToken(modification.ValueToReplace.Path),
                            modification);

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
        /// Hooks up a module input to a <see cref="Reference"/> that gets its value.
        /// </summary>
        /// <param name="referencedModule">The referenced module.</param>
        /// <param name="intrinsicInfo">The intrinsic information.</param>
        /// <param name="reference">The reference.</param>
        private static void ApplyModuleInputReference(
            ModuleInfo referencedModule,
            IntrinsicInfo intrinsicInfo,
            Reference reference)
        {
            // Find input with value matching the intrinsic evaluation
            var index = GetModuleInputIndex(
                referencedModule.Inputs,
                tuple => tuple.Item1.ScalarIdentity == intrinsicInfo.Evaluation.ToString());

            if (index != -1)
            {
                // Overwrite the original scalar input with a new reference.
                referencedModule.Inputs[index] = new ModuleInputVariable(
                    referencedModule.Inputs[index].Name,
                    reference);
            }
        }

        /// <summary>
        /// Applies the new reference or interpolated value to the given JToken.
        /// </summary>
        /// <param name="token">The token to replace.</param>
        /// <param name="modification">The modification data.</param>
        private static void ApplyResourceAttributeReference(JToken token, StateModification modification)
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

        /// <summary>
        /// Gets the index of the module input whose value matches the predicate..
        /// </summary>
        /// <param name="inputs">The module inputs.</param>
        /// <param name="predicate">The predicate to check the values.</param>
        /// <returns>Index of matching input; else -1</returns>
        private static int GetModuleInputIndex(
            IEnumerable<InputVariable> inputs,
            Func<(InputVariable, int), bool> predicate)
        {
            try
            {
                return inputs.WithIndex().First(predicate).Item2;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Given an <see cref="IntrinsicInfo"/>, create a <see cref="Reference"/> for the given module's input.
        /// </summary>
        /// <param name="referencedModule">The referenced module.</param>
        /// <param name="intrinsicInfo">The intrinsic information.</param>
        /// <returns>A <see cref="Reference"/></returns>
        private Reference GetModuleInputReference(ModuleInfo referencedModule, IntrinsicInfo intrinsicInfo)
        {
            Reference reference = null;

            switch (intrinsicInfo.TargetType)
            {
                case IntrinsicTargetType.Input:
                    {
                        // Here it is a !Ref to an input variable.
                        if (intrinsicInfo.Intrinsic is RefIntrinsic refIntrinsic)
                        {
                            reference = new InputVariableReference(refIntrinsic.Reference);
                        }

                        break;
                    }

                case IntrinsicTargetType.Resource:
                    {
                        // Here it points to another resource in the current module
                        // which could be a !Ref or a !GetAtt.
                        switch (intrinsicInfo.Intrinsic)
                        {
                            case RefIntrinsic refIntrinsic:

                                var targetResource =
                                    this.terraformResources.First(tr => tr.Name == refIntrinsic.Reference);

                                reference = new DirectReference(targetResource.Address);
                                break;

                            case GetAttIntrinsic getAttIntrinsic:

                                var referencedResource =
                                    this.terraformResources.First(tr => tr.Name == getAttIntrinsic.LogicalId);

                                var result = getAttIntrinsic.GetTargetValue(
                                    this.template,
                                    referencedResource.ResourceInstance);

                                if (result.Success)
                                {
                                    reference = new IndirectReference(
                                        $"{referencedResource.Type}.{getAttIntrinsic.LogicalId}.{result.TargetAttributePath}");
                                }

                                break;
                        }

                        break;
                    }

                case IntrinsicTargetType.Module:
                    {
                        // Here it must be a !GetAtt pointing to the output of another imported module
                        if (intrinsicInfo.Intrinsic is GetAttIntrinsic getAttIntrinsic)
                        {
                            var attributeName = getAttIntrinsic.GetResolvedAttributeName(this.template);

                            // Remove the "Outputs" qualifier.
                            if (!attributeName.StartsWith(TerraformExporterConstants.StackOutputQualifier))
                            {
                                // Shouldn't get here (famous last words)
                                break;
                            }

                            reference = new ModuleReference(
                                $"{intrinsicInfo.TargetResource.Module.Name}.{attributeName.Substring(TerraformExporterConstants.StackOutputQualifier.Length)}");
                        }

                        break;
                    }

                case IntrinsicTargetType.Unknown:

                    // We have a compound intrinsic like !Select, !Join etc.
                    reference = this.ProcessOtherIntrinsic(intrinsicInfo, referencedModule);
                    break;
            }

            return reference;
        }

        /// <summary>
        /// Processes intrinsic other than <c>!Ref</c> and <c>!GetAtt</c>
        /// </summary>
        /// <param name="intrinsicInfo">The intrinsic info.</param>
        /// <param name="referencedModule">The module being referenced.</param>
        /// <returns>A <see cref="Reference"/> that should be used as the module's input variable value; else <c>null</c>.</returns>
        private Reference ProcessOtherIntrinsic(IntrinsicInfo intrinsicInfo, ModuleInfo referencedModule)
        {
            switch (intrinsicInfo.Intrinsic)
            {
                case JoinIntrinsic joinIntrinsic:

                    {
                        // Does the join evaluation match a scalar input?
                        var scalarEvaluation = joinIntrinsic.Evaluate(intrinsicInfo).ToString();
                        var index = GetModuleInputIndex(
                            referencedModule.Inputs,
                            tuple => tuple.Item1.ScalarIdentity == scalarEvaluation);

                        if (index != -1)
                        {
                            return new JoinFunctionReference(joinIntrinsic, this.template, this.module.Inputs);
                        }

                        if (joinIntrinsic.Separator != ",")
                        {
                            // Should have matched on the scalar evaluation if the separator is anything else.
                            return null;
                        }

                        // A nested CF stack may have a List<...> input, however when passing the values
                        // from the root stack, they have to be passed as a comma separated list.
                        // Make the assumption here that if the join separator is comma, then this is
                        // what is happening.
                        var elements = new List<string>();

                        foreach (var item in joinIntrinsic.Items)
                        {
                            if (item is IIntrinsic intrinsic)
                            {
                                var nested = intrinsic.GetInfo();

                                // ReSharper disable once PossibleNullReferenceException - if null, then it's most likely a bug.
                                elements.Add(nested.Intrinsic.Evaluate(nested).ToString());
                            }
                            else
                            {
                                elements.Add(item.ToString());
                            }
                        }

                        var modifications = new Dictionary<int, InputVariable>();

                        foreach (var (item, itemIndex) in referencedModule.Inputs.WithIndex()
                                     .Where(tuple => tuple.item is StringListInputVariable))
                        {
                            if (item.ListIdentity.OrderBy(i => i).SequenceEqual(elements.OrderBy(e => e)))
                            {
                                // Create a ModuleInputVariable with a list of references
                                var args = new List<object>();

                                foreach (var joinItem in joinIntrinsic.Items)
                                {
                                    if (joinItem is IIntrinsic intrinsic)
                                    {
                                        switch (intrinsic)
                                        {
                                            case RefIntrinsic refIntrinsic:

                                                args.Add(new InputVariableReference(refIntrinsic.Reference));
                                                break;

                                            case GetAttIntrinsic getAttIntrinsic:

                                                args.Add(
                                                    getAttIntrinsic.Render(
                                                        this.template,
                                                        getAttIntrinsic.GetInfo().TargetResource,
                                                        this.module.Inputs));
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        args.Add(joinItem.ToString());
                                    }
                                }

                                modifications.Add(itemIndex, new ModuleInputVariable(item.Name, args.ToArray()));
                            }
                        }

                        foreach (var ind in modifications.Keys)
                        {
                            referencedModule.Inputs[ind] = modifications[ind];
                        }
                    }

                    // We already replaced the module input
                    return null;

                default:

                    // Any other kind of intrinsic needs no special handling
                    return intrinsicInfo.Intrinsic.Render(this.template, intrinsicInfo.TargetResource, this.module.Inputs);
            }
        }
    }
}