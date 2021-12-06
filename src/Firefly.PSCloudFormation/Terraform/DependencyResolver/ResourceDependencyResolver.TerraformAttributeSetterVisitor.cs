namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Amazon.S3.Model;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json.Linq;

    internal partial class ResourceDependencyResolver
    {
        // Walk resource attributes looking for specific value and replace with reference

        private class StateModification
        {
            public StateModification(JValue json, int index, JProperty containingProperty, Reference newReference)
            {
                this.Json = json;
                this.Index = index;
                this.ContainingProperty = containingProperty;
                this.NewReference = newReference;
            }

            public int Index { get;  }

            /// <summary>
            /// Gets the containing property if this modification is within nested JSON, e.g. a policy document.
            /// </summary>
            /// <value>
            /// The containing property if nested JSON, else <c>null</c>.
            /// </value>
            public JProperty ContainingProperty { get; }

            public JValue Json { get; }
            
            public Reference NewReference { get; }
        }

        /// <summary>
        /// 
        /// </summary>
        private class TerraformAttributeSetterContext : IJsonVisitorContext<TerraformAttributeSetterContext>
        {
            public TerraformAttributeSetterContext(
                IReadOnlyCollection<IntrinsicInfo> intrinsicInfo,
                ITemplate template,
                bool allowPartialMatch)
            {
                this.Template = template;
                this.IntrinsicInfos = intrinsicInfo;
                this.AllowPartialMatch = allowPartialMatch;
            }

            /// <summary>
            /// Gets the containing property if this modification is within nested JSON, e.g. a policy document.
            /// </summary>
            /// <value>
            /// The containing property if nested JSON, else <c>null</c>.
            /// </value>
            public JProperty ContainingProperty { get; private set; } = null;

            /// <summary>
            /// Gets a value indicating whether partial matches are allowed, i.e. interpolations
            /// </summary>
            /// <value>
            ///   <c>true</c> if [allow partial match]; otherwise, <c>false</c>.
            /// </value>
            public bool AllowPartialMatch { get; }

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
                // TODO: Need to make a JConstructor against a Firefly.PSCloudFormation.Terraform.State.Reference
                var stringValue = json.Value<string>();

                if (StateFileSerializer.TryGetJson(stringValue, false, "Unknown", "Unknown", out var document))
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
                            new StateModification(json, context.Index, context.ContainingProperty, refIntrinsic.Render(context.Template, intrinsicInfo.ImportedResource)));

                        break;

                    case SelectIntrinsic selectIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                json,
                                context.Index,
                                context.ContainingProperty,
                                selectIntrinsic.Render(context.Template, intrinsicInfo.ImportedResource)));
                        break;

                    case FindInMapIntrinsic findInMapIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                json,
                                context.Index,
                                context.ContainingProperty,
                                findInMapIntrinsic.Render(context.Template, intrinsicInfo.ImportedResource)));
                        break;

                    case GetAttIntrinsic getAttIntrinsic:

                        context.Modifications.Add(
                            new StateModification(
                                json,
                                context.Index,
                                context.ContainingProperty,
                                getAttIntrinsic.Render(context.Template, intrinsicInfo.ImportedResource)));
                        break;
                }
            }
        }
    }
}