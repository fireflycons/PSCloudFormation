namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.CloudFormationParser.Utils;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    internal partial class ResourceDependencyResolver
    {
        /// <summary>
        /// Object to store intrinsic functions extracted by visiting the CloudFormation resource object
        /// </summary>
        [DebuggerDisplay("{PropertyPath.Path}: {Intrinsic}")]
        private class IntrinsicInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="IntrinsicInfo"/> class.
            /// </summary>
            /// <param name="propertyPath">The property path.</param>
            /// <param name="intrinsic">The intrinsic.</param>
            /// <param name="referenceType">Type of reference</param>
            /// <param name="resourceMapping">Summary info of the resource targeted by this intrinsic.</param>
            /// <param name="evaluation">The evaluation.</param>
            /// <param name="substitutionName">substitution key where this object represents a key value pair in a <c>!Sub</c>.</param>
            public IntrinsicInfo(
                PropertyPath propertyPath,
                IIntrinsic intrinsic,
                ReferenceType referenceType,
                ResourceMapping resourceMapping,
                object evaluation,
                string substitutionName)
            {
                this.TargetResource = resourceMapping;
                this.Intrinsic = intrinsic;
                this.PropertyPath = propertyPath.Clone();
                this.Evaluation = evaluation;
                this.ReferenceType = referenceType;
                this.IsScalar = evaluation is string || !(evaluation is IEnumerable);
                this.SubstitutionName = substitutionName;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="IntrinsicInfo"/> class.
            /// </summary>
            /// <param name="propertyPath">The property path.</param>
            /// <param name="intrinsic">The intrinsic.</param>
            /// <param name="referenceType">Type of reference</param>
            /// <param name="resourceMapping">Summary info of the resource targeted by this intrinsic.</param>
            /// <param name="evaluation">The evaluation.</param>
            /// <param name="nestedIntrinsics">For <c>!Sub</c> or <c>!Join</c>, a list of intrinsic nested within.</param>
            public IntrinsicInfo(
                PropertyPath propertyPath,
                IIntrinsic intrinsic,
                ReferenceType referenceType,
                ResourceMapping resourceMapping,
                object evaluation,
                IList<IntrinsicInfo> nestedIntrinsics = null)
            {
                this.TargetResource = resourceMapping;
                this.Intrinsic = intrinsic;
                this.PropertyPath = propertyPath.Clone();
                this.Evaluation = evaluation;
                this.ReferenceType = referenceType;
                this.IsScalar = evaluation is string || !(evaluation is IEnumerable);
                this.NestedIntrinsics = nestedIntrinsics;
            }

            /// <summary>
            /// Gets the substitution key where this object represents
            /// a key value pair in a !Sub.
            /// </summary>
            /// <value>
            /// The name of the substitution.
            /// </value>
            public string SubstitutionName { get; }

            /// <summary>
            /// Gets the evaluation.
            /// </summary>
            /// <value>
            /// The evaluation.
            /// </value>
            public object Evaluation { get; }

            /// <summary>
            /// Gets the summary info of the resource targeted by this intrinsic.
            /// </summary>
            /// <value>
            /// The targeted resource. Will be <c>null</c> when reference is not to a resource.
            /// </value>
            public ResourceMapping TargetResource { get; }

            /// <summary>
            /// Gets the intrinsic.
            /// </summary>
            /// <value>
            /// The intrinsic.
            /// </value>
            public IIntrinsic Intrinsic { get; }

            /// <summary>
            /// Gets the list of nested intrinsic for <c>!Sub</c> or <c>!Join</c>.
            /// </summary>
            /// <value>
            /// The list of nested intrinsic.
            /// </value>
            public IList<IntrinsicInfo> NestedIntrinsics { get; }

            /// <summary>
            /// Gets a value indicating whether this instance is scalar.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is scalar; otherwise, <c>false</c>.
            /// </value>
            public bool IsScalar { get; }
            
            /// <summary>
            /// Gets a value indicating whether this intrinsic is within an inline policy document.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is an inline policy; otherwise, <c>false</c>.
            /// </value>
            public bool IsInlinePolicy => this.PropertyPath.Contains("PolicyDocument");

            /// <summary>
            /// Gets a value indicating whether this intrinsic is within an assume role policy.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is assume role policy; otherwise, <c>false</c>.
            /// </value>
            public bool IsAssumeRolePolicy => this.PropertyPath.Contains("AssumeRolePolicyDocument");

            /// <summary>
            /// Gets the property path.
            /// </summary>
            /// <value>
            /// The property path.
            /// </value>
            public PropertyPath PropertyPath { get; }

            /// <summary>
            /// Gets the type of the reference.
            /// </summary>
            /// <value>
            /// The type of the reference.
            /// </value>
            public ReferenceType ReferenceType { get; }
        }

        /// <summary>
        /// Visits the properties opf a CloudFormation resource extracting intrinsics
        /// we need for dependency resolution
        /// </summary>
        /// <seealso cref="Firefly.CloudFormationParser.TemplateObjects.TemplateObjectVisitor" />
        private class IntrinsicVisitor : TemplateObjectVisitor
        {
            private static readonly Tuple<ReferenceType, ResourceMapping, object> NullTuple =
                new Tuple<ReferenceType, ResourceMapping, object>(ReferenceType.DependsOn, null, null);

            /// <summary>
            /// All CloudFormation resources read from stack
            /// </summary>
            private readonly IReadOnlyCollection<CloudFormationResource> cloudFormationResources;

            /// <summary>
            /// All current CloudFormation parameters with values expressed as terraform input variables.
            /// </summary>
            private readonly IReadOnlyCollection<InputVariable> inputs;

            /// <summary>
            /// All terraform resources read from state file
            /// </summary>
            private readonly IReadOnlyCollection<StateFileResourceDeclaration> terraformResources;

            /// <summary>
            /// Initializes a new instance of the <see cref="IntrinsicVisitor"/> class.
            /// </summary>
            /// <param name="cloudFormationResources">The cloud formation resources.</param>
            /// <param name="terraformResources">The terraform resources.</param>
            /// <param name="inputs">The inputs.</param>
            public IntrinsicVisitor(
                IReadOnlyCollection<CloudFormationResource> cloudFormationResources,
                IReadOnlyCollection<StateFileResourceDeclaration> terraformResources,
                IReadOnlyCollection<InputVariable> inputs)
            {
                this.cloudFormationResources = cloudFormationResources;
                this.terraformResources = terraformResources;
                this.inputs = inputs;
            }

            /// <summary>
            /// Gets the reference locations.
            /// </summary>
            /// <value>
            /// The reference locations.
            /// </value>
            public List<IntrinsicInfo> ReferenceLocations { get; } = new List<IntrinsicInfo>();

            /// <summary>
            /// Called when an intrinsic is located in the resource properties.
            /// Builds a description of the intrinsic needed to generate <see cref="StateModification"/> data.
            /// </summary>
            /// <param name="templateObject">The template object being walked (i.e. IResource)</param>
            /// <param name="path">Current path within properties.</param>
            /// <param name="intrinsic">Intrinsic to inspect.</param>
            /// <returns><c>true</c> if visit should continue within this intrinsic; else <c>false</c></returns>
            public override bool VisitIntrinsic(ITemplateObject templateObject, PropertyPath path, IIntrinsic intrinsic)
            {
                var recurseIntrinsic = true;
                object evaluation;
                var nestedIntrinsics = new List<IntrinsicInfo>();
                ReferenceType referenceType;
                ResourceMapping targetResourceSummary = null;

                switch (intrinsic)
                {
                    case IfIntrinsic _:

                        // Not parsing through conditions at this time
                        // We just take the branch that was selected when the CF was applied.
                        return true;

                    case RefIntrinsic refIntrinsic:
                        {
                            var tuple = this.VisitRef(refIntrinsic);

                            if (tuple.Equals(NullTuple))
                            {
                                return false;
                            }

                            (referenceType, targetResourceSummary, evaluation) = tuple;
                            break;
                        }

                    case GetAttIntrinsic getAttIntrinsic:
                        {
                            var tuple = this.VisitGetAtt(getAttIntrinsic, templateObject);

                            if (tuple.Equals(NullTuple))
                            {
                                return false;
                            }

                            (referenceType, targetResourceSummary, evaluation) = tuple;
                            break;
                        }

                    case SubIntrinsic subIntrinsic:
                        {
                            evaluation = intrinsic.Evaluate(templateObject.Template);
                            referenceType = ReferenceType.DirectReference;

                            foreach (var nestedIntrinsic in subIntrinsic.ImplicitReferences)
                            {
                                Tuple<ReferenceType, ResourceMapping, object> tuple = NullTuple;

                                switch (nestedIntrinsic)
                                {
                                    case RefIntrinsic refIntrinsic:

                                        tuple = this.VisitRef(refIntrinsic);
                                        break;

                                    case GetAttIntrinsic getAttIntrinsic:

                                        tuple = this.VisitGetAtt(getAttIntrinsic, templateObject);
                                        break;
                                }

                                if (!tuple.Equals(NullTuple))
                                {
                                    nestedIntrinsics.Add(
                                        new IntrinsicInfo(
                                            path,
                                            (IIntrinsic)nestedIntrinsic,
                                            tuple.Item1,
                                            tuple.Item2,
                                            tuple.Item3));
                                }
                            }

                            foreach (var kvp in subIntrinsic.Substitutions)
                            {
                                Tuple<ReferenceType, ResourceMapping, object> tuple = NullTuple;

                                switch (kvp.Value)
                                {
                                    case RefIntrinsic refIntrinsic:

                                        tuple = this.VisitRef(refIntrinsic);
                                        break;

                                    case GetAttIntrinsic getAttIntrinsic:

                                        tuple = this.VisitGetAtt(getAttIntrinsic, templateObject);
                                        break;

                                    case FindInMapIntrinsic findInMapIntrinsic:

                                        tuple = new Tuple<ReferenceType, ResourceMapping, object>(
                                            ReferenceType.DirectReference,
                                            null,
                                            findInMapIntrinsic.Evaluate(templateObject.Template));
                                        break;
                                }

                                if (!tuple.Equals(NullTuple))
                                {
                                    nestedIntrinsics.Add(
                                        new IntrinsicInfo(
                                            path,
                                            (IIntrinsic)kvp.Value,
                                            tuple.Item1,
                                            tuple.Item2,
                                            tuple.Item3,
                                            kvp.Key));
                                }
                            }
                        }

                        recurseIntrinsic = false;
                        break;

                    case JoinIntrinsic joinIntrinsic:
                        {
                            evaluation = intrinsic.Evaluate(templateObject.Template);
                            referenceType = ReferenceType.DirectReference;

                            foreach (var nestedIntrinsic in joinIntrinsic.Items.Where(i => i is IIntrinsic).Cast<IIntrinsic>())
                            {
                                Tuple<ReferenceType, ResourceMapping, object> tuple = NullTuple;

                                switch (nestedIntrinsic)
                                {
                                    case RefIntrinsic refIntrinsic:

                                        tuple = this.VisitRef(refIntrinsic);
                                        break;

                                    case GetAttIntrinsic getAttIntrinsic:

                                        tuple = this.VisitGetAtt(getAttIntrinsic, templateObject);
                                        break;

                                    case FindInMapIntrinsic findInMapIntrinsic:

                                        tuple = new Tuple<ReferenceType, ResourceMapping, object>(
                                            ReferenceType.DirectReference,
                                            null,
                                            findInMapIntrinsic.Evaluate(templateObject.Template));
                                        break;
                                }

                                if (!tuple.Equals(NullTuple))
                                {
                                    nestedIntrinsics.Add(
                                        new IntrinsicInfo(
                                            path,
                                            nestedIntrinsic,
                                            tuple.Item1,
                                            tuple.Item2,
                                            tuple.Item3));
                                }
                            }

                            recurseIntrinsic = false;
                            break;
                        }

                    default:

                        evaluation = intrinsic.Evaluate(templateObject.Template);
                        referenceType = ReferenceType.DirectReference;
                        break;
                }

                this.ReferenceLocations.Add(
                    new IntrinsicInfo(path, intrinsic, referenceType, targetResourceSummary, evaluation, nestedIntrinsics));

                return recurseIntrinsic;
            }

            private Tuple<ReferenceType, ResourceMapping, object> VisitRef(
                RefIntrinsic refIntrinsic)
            {
                ReferenceType referenceType;
                object evaluation;
                var target = refIntrinsic.Reference;

                var param = this.inputs.FirstOrDefault(i => i.Name == target);

                if (param != null)
                {
                    evaluation = param.IsScalar ? (object)param.ScalarIdentity : param.ListIdentity;
                    referenceType = ReferenceType.ParameterReference;
                    return new Tuple<ReferenceType, ResourceMapping, object>(referenceType, null, evaluation);
                }

                if (target.StartsWith("AWS::"))
                {
                    // An unsupported AWS pseudo parameter like AWS::StackName etc.
                    return NullTuple;
                }

                var cloudFormationResource =
                    this.cloudFormationResources
                        .Where(r => TerraformExporter.IgnoredResources.All(ir => ir != r.ResourceType))
                        .FirstOrDefault(r => r.LogicalResourceId == target);

                if (cloudFormationResource == null)
                {
                    // If not found, then reference is to a resource that couldn't be imported eg. a custom resource
                    // or a known unsupported type.
                    return NullTuple;
                }

                referenceType = ReferenceType.DirectReference;


                var targetResourceSummary = new ResourceMapping
                                                {
                                                    AwsType = cloudFormationResource.ResourceType,
                                                    LogicalId = cloudFormationResource.LogicalResourceId,
                                                    PhysicalId = cloudFormationResource.PhysicalResourceId,
                                                    TerraformType = this.terraformResources.First(
                                                        tr => tr.Name == cloudFormationResource
                                                                  .LogicalResourceId).Type
                                                };

                evaluation = cloudFormationResource.PhysicalResourceId;

                return new Tuple<ReferenceType, ResourceMapping, object>(referenceType, targetResourceSummary, evaluation);
            }

            private Tuple<ReferenceType, ResourceMapping, object> VisitGetAtt(
                GetAttIntrinsic getAttIntrinsic,
                ITemplateObject templateObject)
            {
                object evaluation;

                // Logical name of the resource being referenced by this !GetAtt
                var (referencedResourceName, attribute) =
                    (Tuple<string, string>)getAttIntrinsic.Evaluate(templateObject.Template);

                // State file instance of the resource being referenced by this !GetAtt
                var referencedResouce = this.terraformResources
                    .FirstOrDefault(r => r.Name == referencedResourceName)?.Instances.First();

                if (referencedResouce == null)
                {
                    // If not found, then reference is to a resource that couldn't be imported eg. a custom resource.
                    return NullTuple;
                }

                // CloudFormation instance of the resource being referenced by this !GetAtt
                var cloudFormationResource =
                    this.cloudFormationResources.First(
                        r => r.LogicalResourceId == getAttIntrinsic.LogicalId);

                // Summary of the resource to which this !GetAtt refers to
                var targetResourceSummary = new ResourceMapping
                                                {
                                                    AwsType = cloudFormationResource.ResourceType,
                                                    LogicalId = cloudFormationResource.LogicalResourceId,
                                                    PhysicalId = cloudFormationResource.PhysicalResourceId,
                                                    TerraformType = this.terraformResources.First(
                                                        tr => tr.Name == cloudFormationResource.LogicalResourceId).Type
                                                };

                // Now attempt to match up the CloudFormation resource attribute name with the corresponding terraform one
                // and get the current value from state.
                //
                // First, look up the attribute map
                var traits = ResourceTraitsCollection.Get(referencedResouce.Parent.Type);

                if (traits.AttributeMap.ContainsKey(attribute))
                {
                    evaluation = traits.AttributeMap[attribute];
                    
                }
                else
                {
                    var context = new TerraformAttributeGetterContext(attribute);
                    referencedResouce.Attributes.Accept(new TerraformAttributeGetterVisitor(), context);
                    evaluation = context.Value;
                }

                return new Tuple<ReferenceType, ResourceMapping, object>(ReferenceType.AttributeReference, targetResourceSummary, evaluation);
            }
        }
    }
}