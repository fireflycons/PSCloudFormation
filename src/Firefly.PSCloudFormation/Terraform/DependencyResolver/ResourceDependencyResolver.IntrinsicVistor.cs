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
            /// <param name="importedResource"></param>
            /// <param name="evaluation">The evaluation.</param>
            public IntrinsicInfo(
                PropertyPath propertyPath,
                IIntrinsic intrinsic,
                ReferenceType referenceType,
                ImportedResource importedResource,
                object evaluation)
            {
                this.ImportedResource = importedResource;
                this.Intrinsic = intrinsic;
                this.PropertyPath = propertyPath.Clone();
                this.Evaluation = evaluation;
                this.ReferenceType = referenceType;
                this.IsScalar = evaluation is string || !(evaluation is IEnumerable);
            }

            /// <summary>
            /// Gets the evaluation.
            /// </summary>
            /// <value>
            /// The evaluation.
            /// </value>
            public object Evaluation { get; }

            /// <summary>
            /// Gets the imported resource.
            /// </summary>
            /// <value>
            /// The imported resource. Will be <c>null</c> when reference is to a parameter (input variable)
            /// </value>
            public ImportedResource ImportedResource { get; }

            /// <summary>
            /// Gets the intrinsic.
            /// </summary>
            /// <value>
            /// The intrinsic.
            /// </value>
            public IIntrinsic Intrinsic { get; }

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
            /// Called when an intrinsic is located in the resource properties
            /// </summary>
            /// <param name="templateObject">The template object being walked (i.e. IResource)</param>
            /// <param name="path">Current path within properties.</param>
            /// <param name="intrinsic">Intrinsic to inspect.</param>
            /// <inheritdoc />
            public override void VisitIntrinsic(ITemplateObject templateObject, PropertyPath path, IIntrinsic intrinsic)
            {
                object evaluation = null;
                ReferenceType referenceType;
                ImportedResource importedResource = null;

                switch (intrinsic)
                {
                    case IfIntrinsic _:

                        // Not parsing through conditions at this time
                        return;

                    case RefIntrinsic refIntrinsic:
                        {
                            var target = refIntrinsic.Reference;

                            var param = this.inputs.FirstOrDefault(i => i.Name == target);

                            if (param != null)
                            {
                                evaluation = param.IsScalar ? (object)param.ScalarIdentity : param.ListIdentity;
                                referenceType = ReferenceType.ParameterReference;
                                break;
                            }

                            if (target.StartsWith("AWS::"))
                            {
                                // An unsupported AWS pseudo parameter like AWS::StackName etc.
                                return;
                            }

                            var cloudFormationResource =
                                this.cloudFormationResources
                                    .Where(r => TerraformExporter.IgnoredResources.All(ir => ir != r.ResourceType))
                                    .FirstOrDefault(r => r.LogicalResourceId == target);

                            if (cloudFormationResource == null)
                            {
                                // If not found, then reference is to a resource that couldn't be imported eg. a custom resource
                                // or a known unsupported type.
                                return;
                            }

                            referenceType = ReferenceType.DirectReference;


                            importedResource = new ImportedResource
                                                   {
                                                       AwsType = cloudFormationResource.ResourceType,
                                                       LogicalId = cloudFormationResource.LogicalResourceId,
                                                       PhysicalId = cloudFormationResource.PhysicalResourceId,
                                                       TerraformType = this.terraformResources.First(
                                                           tr => tr.Name == cloudFormationResource
                                                                     .LogicalResourceId).Type
                                                   };

                            evaluation = cloudFormationResource.PhysicalResourceId;
                            break;
                        }

                    case GetAttIntrinsic getAttIntrinsic:
                        {
                            referenceType = ReferenceType.AttributeReference;
                            var (referencedResourceName, attribute) =
                                (Tuple<string, string>)getAttIntrinsic.Evaluate(templateObject.Template);

                            var referencedResouce = this.terraformResources
                                .FirstOrDefault(r => r.Name == referencedResourceName)?.Instances.First();

                            if (referencedResouce == null)
                            {
                                // If not found, then reference is to a resource that couldn't be imported eg. a custom resource.
                                return;
                            }

                            var cloudFormationResource =
                                this.cloudFormationResources.First(
                                    r => r.LogicalResourceId == getAttIntrinsic.LogicalId);

                            importedResource = new ImportedResource
                                                   {
                                                       AwsType = cloudFormationResource.ResourceType,
                                                       LogicalId = cloudFormationResource.LogicalResourceId,
                                                       PhysicalId = cloudFormationResource.PhysicalResourceId,
                                                       TerraformType = this.terraformResources.First(
                                                               tr => tr.Name == cloudFormationResource
                                                                         .LogicalResourceId)
                                                           .Type
                                                   };

                            // First, look up the attribute map
                            var traits = ResourceTraitsCollection.Get(referencedResouce.Parent.Type);

                            if (traits.AttributeMap.ContainsKey(attribute))
                            {
                                evaluation = traits.AttributeMap[attribute];
                                break;
                            }

                            var context = new TerraformAttributeGetterContext(attribute);
                            referencedResouce.Attributes.Accept(new TerraformAttributeGetterVisitor(), context);
                            evaluation = context.Value;

                            break;
                        }

                    default:

                        evaluation = intrinsic.Evaluate(templateObject.Template);
                        referenceType = ReferenceType.DirectReference;
                        break;
                }

                this.ReferenceLocations.Add(
                    new IntrinsicInfo(path, intrinsic, referenceType, importedResource, evaluation));
            }
        }
    }
}