namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.PSCloudFormation.Terraform.Hcl;
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
            /// <param name="reference">The reference.</param>
            public StateModification(
                JValue valueToReplace,
                int index,
                JProperty containingProperty,
                Reference reference)
                : this(valueToReplace, index, containingProperty)
            {
                this.Reference = reference;
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
            /// Gets the containing property if this modification is within nested JSON, e.g. a policy document.
            /// </summary>
            /// <value>
            /// The containing property if nested JSON, else <c>null</c>.
            /// </value>
            public JProperty ContainingProperty { get; }

            /// <summary>
            /// Gets the index into the array where the value resides
            /// where the value being replaced is a member of a JArray.
            /// </summary>
            /// <value>
            /// The index.
            /// </value>
            public int Index { get; }

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
            /// Gets the specific JValue that will be replaced with a reference or interpolation.
            /// </summary>
            /// <value>
            /// The value to replace.
            /// </value>
            public JValue ValueToReplace { get; }
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
            /// <param name="inputs">The list of input variables and data sources.</param>
            public TerraformAttributeSetterContext(
                IReadOnlyCollection<IntrinsicInfo> intrinsicInfo,
                ITemplate template,
                StateFileResourceDeclaration resource,
                IList<InputVariable> inputs)
            {
                this.Template = template;
                this.IntrinsicInfos = intrinsicInfo;
                this.Resource = resource;
                this.ResourceTraits = ResourceTraitsCollection.Get(resource.Type);
                this.Inputs = inputs;
            }

            /// <summary>
            /// Gets the inputs.
            /// </summary>
            /// <value>
            /// The inputs.
            /// </value>
            public IList<InputVariable> Inputs { get; }

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
            /// <summary>
            /// Visits a boolean value in the JSON object graph.
            /// Look to see if this matches the evaluation of any CloudFormation intrinsic
            /// and generate a <see cref="StateModification"/> where a match is found.
            /// </summary>
            /// <param name="jsonValue">A JValue of type boolean.</param>
            /// <param name="context">The visitor context.</param>
            protected override void VisitBoolean(JValue jsonValue, TerraformAttributeSetterContext context)
            {
                if (context.ResourceTraits.ComputedAttributes.Any(a => a.IsLike(GetParentPropertyKey(jsonValue))))
                {
                    // Don't adjust computed attributes
                    return;
                }

                var boolValue = jsonValue.Value<bool>();

                var intrinsicInfo = context.IntrinsicInfos.FirstOrDefault(i => i.Evaluation is bool b1 && b1 == boolValue);

                if (intrinsicInfo == null)
                {
                    // No intrinsic evaluation matches this JValue
                    return;
                }

                CreateModification(jsonValue, context, intrinsicInfo);
            }

            /// <summary>
            /// Visits a float/double value in the JSON object graph.
            /// Look to see if this matches the evaluation of any CloudFormation intrinsic
            /// and generate a <see cref="StateModification"/> where a match is found.
            /// </summary>
            /// <param name="jsonValue">A JValue of type float.</param>
            /// <param name="context">The visitor context.</param>
            protected override void VisitFloat(JValue jsonValue, TerraformAttributeSetterContext context)
            {
                if (context.ResourceTraits.ComputedAttributes.Any(a => a.IsLike(GetParentPropertyKey(jsonValue))))
                {
                    // Don't adjust computed attributes
                    return;
                }

                CreateNumericModification(jsonValue, context, jsonValue.Value<double>());
            }

            /// <summary>
            /// Visits an integer value in the JSON object graph.
            /// Look to see if this matches the evaluation of any CloudFormation intrinsic
            /// and generate a <see cref="StateModification"/> where a match is found.
            /// </summary>
            /// <param name="jsonValue">A JValue of type integer.</param>
            /// <param name="context">The visitor context.</param>
            protected override void VisitInteger(JValue jsonValue, TerraformAttributeSetterContext context)
            {
                if (context.ResourceTraits.ComputedAttributes.Any(a => a.IsLike(GetParentPropertyKey(jsonValue))))
                {
                    // Don't adjust computed attributes
                    return;
                }

                CreateNumericModification(jsonValue, context, jsonValue.Value<double>());
            }

            /// <summary>
            /// Visits a string value in the JSON object graph.
            /// Look to see if this matches the evaluation of any CloudFormation intrinsic
            /// and generate a <see cref="StateModification"/> where a match is found.
            /// </summary>
            /// <param name="jsonValue">A JValue of type string.</param>
            /// <param name="context">The visitor context.</param>
            protected override void VisitString(JValue jsonValue, TerraformAttributeSetterContext context)
            {
                if (context.ResourceTraits.ComputedAttributes.Any(a => a.IsLike(GetParentPropertyKey(jsonValue))))
                {
                    // Don't adjust computed attributes
                    return;
                }

                var stringValue = jsonValue.Value<string>();

                if (StateFileSerializer.TryGetJson(
                    stringValue,
                    false,
                    context.Resource.Name,
                    context.Resource.Type,
                    out var document))
                {
                    try
                    {
                        context.EnterNestedJson((JProperty)jsonValue.Parent);

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

                CreateModification(jsonValue, context, intrinsicInfo);
            }

            /// <summary>
            /// Creates a modification record.
            /// </summary>
            /// <param name="jsonValue">The JSON value that will be replaced.</param>
            /// <param name="context">The context.</param>
            /// <param name="intrinsicInfo">The intrinsic information.</param>
            private static void CreateModification(
                JValue jsonValue,
                TerraformAttributeSetterContext context,
                IntrinsicInfo intrinsicInfo)
            {
                var intrinsic = intrinsicInfo.Intrinsic;

                switch (intrinsic.Type)
                {
                    case IntrinsicType.Base64:
                    case IntrinsicType.Cidr:
                    case IntrinsicType.Split:
                    case IntrinsicType.Ref:
                    case IntrinsicType.Select:
                    case IntrinsicType.FindInMap:
                    case IntrinsicType.GetAtt:
                    case IntrinsicType.Join:
                    case IntrinsicType.Sub:

                        var reference = intrinsic.Render(context.Template, intrinsicInfo.TargetResource);

                        if (reference is DataSourceReference ds && !ds.IsParameter)
                        {
                            if (!context.Inputs.Any(i => i.IsDataSource && i.Address == ds.BlockAddress))
                            {
                                context.Inputs.Add(new DataSourceInput(ds.Type, ds.Name));
                            }
                        }

                        context.Modifications.Add(
                            new StateModification(
                                jsonValue,
                                context.Index,
                                context.ContainingProperty,
                                reference));
                        break;
                }
            }

            /// <summary>
            /// Creates a modification records for numeric types.
            /// </summary>
            /// <param name="jsonValue">The JSON value that will be replaced.</param>
            /// <param name="context">The context.</param>
            /// <param name="doubleVal">The double value.</param>
            private static void CreateNumericModification(
                JValue jsonValue,
                TerraformAttributeSetterContext context,
                double doubleVal)
            {
                var intrinsicInfo = context.IntrinsicInfos.FirstOrDefault(
                    i => double.TryParse(i.Evaluation.ToString(), out var d) && Math.Abs(d - doubleVal) < 0.001);

                if (intrinsicInfo == null)
                {
                    // No intrinsic evaluation matches this JValue
                    return;
                }

                CreateModification(jsonValue, context, intrinsicInfo);
            }

            /// <summary>
            /// From the given token, walk upwards to find containing JProperty's name.
            /// </summary>
            /// <param name="token">The JToken where the modification will be applied.</param>
            /// <returns>Name of the containing property.</returns>
            private static string GetParentPropertyKey(JToken token)
            {
                if (token == null)
                {
                    throw new ArgumentNullException(nameof(token));
                }

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