namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Amazon.S3.Model;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics.Abstractions;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json.Linq;

    internal partial class ResourceDependencyResolver
    {
        private enum StateModificationType
        {
            DirectReference,

            Interpolated
        }

        // Walk resource attributes looking for specific value and replace with reference

        private class StateModification
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StateModification"/> class.
            /// </summary>
            /// <param name="valueToReplace">The value to replace.</param>
            /// <param name="index">The index.</param>
            /// <param name="containingProperty">The containing property if this modification is within nested JSON, e.g. a policy document.</param>
            /// <param name="interpolation">The interpolation.</param>
            public StateModification(JValue valueToReplace, int index, JProperty containingProperty, string interpolation)
                : this(valueToReplace, index, containingProperty)
            {
                this.Interpolation = new JValue(interpolation);
                this.Type = StateModificationType.Interpolated;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="StateModification"/> class.
            /// </summary>
            /// <param name="valueToReplace">The value to replace.</param>
            /// <param name="index">The index.</param>
            /// <param name="containingProperty">The containing property if this modification is within nested JSON, e.g. a policy document.</param>
            /// <param name="reference">The reference.</param>
            public StateModification(JValue valueToReplace, int index, JProperty containingProperty, Reference reference)
                : this(valueToReplace, index, containingProperty)
            {
                this.Reference = reference;
                this.Type = StateModificationType.DirectReference;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="StateModification"/> class.
            /// </summary>
            /// <param name="valueToReplace">The value to replace.</param>
            /// <param name="index">The index.</param>
            /// <param name="containingProperty">The containing property if this modification is within nested JSON, e.g. a policy document.</param>
            private StateModification(JValue valueToReplace, int index, JProperty containingProperty)
            {
                this.ValueToReplace = valueToReplace;
                this.Index = index;
                this.ContainingProperty = containingProperty;
            }

            /// <summary>
            /// Gets the type of modification to do to the original value.
            /// </summary>
            /// <value>
            /// The type.
            /// </value>
            public StateModificationType Type { get; }

            /// <summary>
            /// Gets the index into the array where the value resides
            /// where the value being replaced is a member of a JArray.
            /// </summary>
            /// <value>
            /// The index.
            /// </value>
            public int Index { get;  }

            /// <summary>
            /// Gets the containing property if this modification is within nested JSON, e.g. a policy document.
            /// </summary>
            /// <value>
            /// The containing property if nested JSON, else <c>null</c>.
            /// </value>
            public JProperty ContainingProperty { get; }

            /// <summary>
            /// Gets the specific JValue that will be replaced with a reference or interpolation.
            /// </summary>
            /// <value>
            /// The value to replace.
            /// </value>
            public JValue ValueToReplace { get; }

            /// <summary>
            /// Gets a <see cref="Reference"/> to set on the target property.
            /// This will be encoded within a JConstructor which is
            /// deserialized by the <see cref="HclEmitter"/> to an HCL expression.
            /// </summary>
            /// <value>
            /// The reference.
            /// </value>
            public Reference Reference { get; }

            /// <summary>
            /// Gets a string interpolation to set on the target property, as a result of a !Sub or !Join.
            /// This is simply set as a replacement JValue. 
            /// </summary>
            /// <value>
            /// The interpolation.
            /// </value>
            public JValue Interpolation { get; }
        }

        /// <summary>
        /// 
        /// </summary>
        private class TerraformAttributeSetterContext : IJsonVisitorContext<TerraformAttributeSetterContext>
        {
            public TerraformAttributeSetterContext(
                IReadOnlyCollection<IntrinsicInfo> intrinsicInfo,
                ITemplate template,
                StateFileResourceDeclaration resource)
            {
                this.Template = template;
                this.IntrinsicInfos = intrinsicInfo;
                this.Resource = resource;
                this.ResourceTraits = ResourceTraitsCollection.Get(resource.Type);
            }
            
            /// <summary>
            /// Gets the resource being visited.
            /// </summary>
            /// <value>
            /// The resource.
            /// </value>
            public StateFileResourceDeclaration Resource { get; }

            /// <summary>
            /// Gets the resource traits for the resource being visited.
            /// </summary>
            /// <value>
            /// The resource traits.
            /// </value>
            public IResourceTraits ResourceTraits { get; }

            /// <summary>
            /// Gets the containing property if this modification is within nested JSON, e.g. a policy document.
            /// </summary>
            /// <value>
            /// The containing property if nested JSON, else <c>null</c>.
            /// </value>
            public JProperty ContainingProperty { get; private set; } = null;

            /// <summary>
            /// Gets the index of an item when iterating through a JArray.
            /// </summary>
            /// <value>
            /// The index.
            /// </value>
            public int Index { get; private set; }

            /// <summary>
            /// Gets the intrinsic information.
            /// </summary>
            /// <value>
            /// The intrinsic information.
            /// </value>
            public IReadOnlyCollection<IntrinsicInfo> IntrinsicInfos { get; }
            
            public List<StateModification> Modifications { get; } = new List<StateModification>();

            public ITemplate Template { get; }

            public void EnterNestedJson(JProperty containingProperty)
            {
                this.ContainingProperty = containingProperty;
            }

            public void ExitNestedJson()
            {
                this.ContainingProperty = null;
            }

            public TerraformAttributeSetterContext Next(int index)
            {
                this.Index = index;
                return this;
            }

            public TerraformAttributeSetterContext Next(string name)
            {
                return this;
            }
        }

        /// <summary>
        /// Visit each attribute in the current state file resource examining current values and checking if they match the evaluation
        /// of any intrinsic in the passed context, those being the intrinsics found on the corresponding CloudFormation resource.
        /// Where a match is found, record this and the JSON path to the located attribute such that the value may be replaced with
        /// a JSON encoded <see cref="Reference"/> in a further pass. Can't set that here or will break the iteration.
        /// </summary>
        private class TerraformAttributeSetterVisitor : JValueVisitor<TerraformAttributeSetterContext>
        {
            protected override void VisitBoolean(JValue json, TerraformAttributeSetterContext context)
            {
                base.VisitBoolean(json, context);
            }

            protected override void VisitFloat(JValue json, TerraformAttributeSetterContext context)
            {
                base.VisitFloat(json, context);
            }

            protected override void VisitInteger(JValue json, TerraformAttributeSetterContext context)
            {
                base.VisitInteger(json, context);
            }

            /// <summary>
            /// Visits a string.
            /// </summary>
            /// <param name="json">The string.</param>
            /// <param name="context">The context.</param>
            protected override void VisitString(JValue json, TerraformAttributeSetterContext context)
            {
                if (context.ResourceTraits.ComputedAttributes.Any(a => a.IsLike(this.GetParentPropertyKey(json))))
                {
                    // Don't adjust computed attributes
                    return;
                }

                var stringValue = json.Value<string>();

                if (StateFileSerializer.TryGetJson(stringValue, false, context.Resource.Name, context.Resource.Type, out var document))
                {
                    try
                    {
                        context.EnterNestedJson((JProperty)json.Parent);

                        // Build a new visitor to visit the nested JSON document
                        document.Accept(new TerraformAttributeSetterVisitor(), context);
                    }
                    finally
                    {
                        context.ExitNestedJson();
                    }

                    return;
                }

                var intrinsicInfo = context.IntrinsicInfos.FirstOrDefault(i => i.Evaluation.ToString() == stringValue);

                if (intrinsicInfo == null)
                {
                    return;
                }

                switch (intrinsicInfo.Intrinsic)
                {
                    case RefIntrinsic refIntrinsic:

                        context.Modifications.Add(
                            new StateModification(json, context.Index, context.ContainingProperty, refIntrinsic.Render(context.Template, intrinsicInfo.TargetResource)));

                        break;

                    case SelectIntrinsic selectIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                json,
                                context.Index,
                                context.ContainingProperty,
                                selectIntrinsic.Render(context.Template, intrinsicInfo.TargetResource)));
                        break;

                    case FindInMapIntrinsic findInMapIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                json,
                                context.Index,
                                context.ContainingProperty,
                                findInMapIntrinsic.Render(context.Template, intrinsicInfo.TargetResource)));
                        break;

                    case GetAttIntrinsic getAttIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                json,
                                context.Index,
                                context.ContainingProperty,
                                getAttIntrinsic.Render(context.Template, intrinsicInfo.TargetResource)));
                        break;

                    case SubIntrinsic subIntrinsic:

                        {
                            var expression = subIntrinsic.Expression;

                            foreach (var nestedIntrinsic in intrinsicInfo.NestedIntrinsics)
                            {
                                var reference = nestedIntrinsic.Intrinsic.Render(
                                    context.Template,
                                    nestedIntrinsic.TargetResource);

                                if (reference != null)
                                {
                                    string key = null;

                                    if (nestedIntrinsic.SubstitutionName != null)
                                    {
                                        key = nestedIntrinsic.SubstitutionName;
                                    }
                                    else if (nestedIntrinsic.Intrinsic is IReferenceIntrinsic referenceIntrinsic)
                                    {
                                        key = referenceIntrinsic.ReferencedObject(context.Template);
                                    }

                                    if (key != null)
                                    {
                                        expression = expression.Replace(
                                            $"${{{key}}}",
                                            $"${{{reference.ReferenceExpression}}}");
                                    }
                                }
                            }

                            context.Modifications.Add(
                                new StateModification(json, context.Index, context.ContainingProperty, expression));
                            break;
                        }
                }
            }

            private string GetParentPropertyKey(JToken json)
            {
                var tok = json;

                while (tok.Type != JTokenType.Property)
                {
                    tok = tok.Parent;
                }

                return ((JProperty)tok).Name;
            }
        }
    }
}