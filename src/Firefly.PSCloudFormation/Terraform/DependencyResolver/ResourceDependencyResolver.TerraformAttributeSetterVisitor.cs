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

    /// <content>
    /// This part handles the location of attributes in the in-memory copy of the state file
    /// that should be updated with references or interpolations, and generates a list
    /// of modifications to apply in a later pass over the resource data.
    /// </content>
    internal partial class ResourceDependencyResolver
    {
        /// <summary>
        /// Type of modification to be made
        /// </summary>
        private enum StateModificationType
        {
            /// <summary>
            /// Modification to state attribute will be a direct reference expression to a resource or input variable.
            /// The <see cref="Reference"/> object is JSON encoded as a JConstructor.
            /// </summary>
            DirectReference,

            /// <summary>
            /// Modification to state attribute will be a replacement of the original string
            /// property value with an interpolated string.
            /// </summary>
            Interpolated
        }

        // Walk resource attributes looking for specific value and replace with reference

        /// <summary>
        /// Contains the attributes necessary to implement a modification to the in-memory copy of the state file.
        /// </summary>
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
        /// Context class for <see cref="TerraformAttributeSetterVisitor" />
        /// </summary>
        private class TerraformAttributeSetterContext : IJsonVisitorContext<TerraformAttributeSetterContext>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TerraformAttributeSetterContext"/> class.
            /// </summary>
            /// <param name="intrinsicInfo">The intrinsic information.</param>
            /// <param name="template">Reference to parsed CloudFormation template.</param>
            /// <param name="resource">Reference to resource being updated.</param>
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
            public JProperty ContainingProperty { get; private set; }

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

            /// <summary>
            /// Gets the list of modifications to be applied to the resource,
            /// which is generated by applying <see cref="TerraformAttributeSetterVisitor"/>.
            /// </summary>
            /// <value>
            /// The modifications.
            /// </value>
            public List<StateModification> Modifications { get; } = new List<StateModification>();

            /// <summary>
            /// Gets a reference to the parsed CloudFormation template.
            /// </summary>
            /// <value>
            /// The template.
            /// </value>
            public ITemplate Template { get; }

            /// <summary>
            /// Where nested JSON like policy documents is found, call this before deserializing and visiting that JSON
            /// to set the property that has this JSON as its value.
            /// </summary>
            /// <param name="containingProperty">The containing property.</param>
            public void EnterNestedJson(JProperty containingProperty)
            {
                this.ContainingProperty = containingProperty;
            }

            /// <summary>
            /// Call this when done visiting nested JSON to clear <see cref="ContainingProperty"/>.
            /// </summary>
            public void ExitNestedJson()
            {
                this.ContainingProperty = null;
            }

            /// <summary>
            /// Gets the next context for an item in an Array.
            /// Store the current index
            /// </summary>
            /// <param name="index">Index in current JArray</param>
            /// <returns>
            /// Current or new context.
            /// </returns>
            public TerraformAttributeSetterContext Next(int index)
            {
                this.Index = index;
                return this;
            }

            /// <summary>
            /// Gets the next context for a property on an Object.
            /// </summary>
            /// <param name="name">Name of property.</param>
            /// <returns>
            /// Current or new context.
            /// </returns>
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
            /// Visits a string value in the JSON object graph.
            /// Look to see if this matches the evaluation of any CloudFormation intrinsic
            /// and generate a <see cref="StateModification"/> where a match is found.
            /// </summary>
            /// <param name="jsonStringValue">A JValue of type string.</param>
            /// <param name="context">The visitor context.</param>
            protected override void VisitString(JValue jsonStringValue, TerraformAttributeSetterContext context)
            {
                if (context.ResourceTraits.ComputedAttributes.Any(a => a.IsLike(this.GetParentPropertyKey(jsonStringValue))))
                {
                    // Don't adjust computed attributes
                    return;
                }

                var stringValue = jsonStringValue.Value<string>();

                if (StateFileSerializer.TryGetJson(stringValue, false, context.Resource.Name, context.Resource.Type, out var document))
                {
                    try
                    {
                        context.EnterNestedJson((JProperty)jsonStringValue.Parent);

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
                    // No intrinsic evaluation matches this JValue
                    return;
                }

                switch (intrinsicInfo.Intrinsic)
                {
                    case RefIntrinsic refIntrinsic:

                        context.Modifications.Add(
                            new StateModification(jsonStringValue, context.Index, context.ContainingProperty, refIntrinsic.Render(context.Template, intrinsicInfo.TargetResource)));

                        break;

                    case SelectIntrinsic selectIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                jsonStringValue,
                                context.Index,
                                context.ContainingProperty,
                                selectIntrinsic.Render(context.Template, intrinsicInfo.TargetResource)));
                        break;

                    case FindInMapIntrinsic findInMapIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                jsonStringValue,
                                context.Index,
                                context.ContainingProperty,
                                findInMapIntrinsic.Render(context.Template, intrinsicInfo.TargetResource)));
                        break;

                    case GetAttIntrinsic getAttIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                jsonStringValue,
                                context.Index,
                                context.ContainingProperty,
                                getAttIntrinsic.Render(context.Template, intrinsicInfo.TargetResource)));
                        break;

                    case SubIntrinsic subIntrinsic:

                        {
                            // Build up an interpolated string as the replacement
                            // Start with the !Sub intrinsic expression.
                            var expression = subIntrinsic.Expression;

                            // Go through any intrinsics associated with this !Sub
                            foreach (var nestedIntrinsic in intrinsicInfo.NestedIntrinsics)
                            {
                                // Try to render to an HCL expression
                                var reference = nestedIntrinsic.Intrinsic.Render(
                                    context.Template,
                                    nestedIntrinsic.TargetResource);

                                if (reference == null)
                                {
                                    continue;
                                }

                                // We got one, so do a string replace in the !Sub expression, 
                                // replacing the placeholder with the rendered HCL expression.
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

                            // Add interpolation modification.
                            context.Modifications.Add(
                                new StateModification(jsonStringValue, context.Index, context.ContainingProperty, expression));
                            break;
                        }
                }
            }

            /// <summary>
            /// From the given token, walk upwards to find containing JProperty's name.
            /// </summary>
            /// <param name="token">The JToken where the modification will be applied.</param>
            /// <returns>Name of the containing property.</returns>
            private string GetParentPropertyKey(JToken token)
            {
                var tok = token;

                while (tok.Type != JTokenType.Property)
                {
                    tok = tok.Parent;
                }

                return ((JProperty)tok).Name;
            }
        }
    }
}