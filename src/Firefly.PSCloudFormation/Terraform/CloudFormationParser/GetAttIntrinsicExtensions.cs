namespace Firefly.PSCloudFormation.Terraform.CloudFormationParser
{
    using System;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform.DependencyResolver;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    /// <summary>
    /// Extra extension methods for <see cref="GetAttIntrinsic"/>
    /// </summary>
    internal static class GetAttIntrinsicExtensions
    {
        /// <summary>
        /// Gets the resolved name of the target attribute.
        /// </summary>
        /// <param name="self">Instance of intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <returns>Resolved name of attribute targeted by this intrinsic.</returns>
        public static string GetResolvedAttributeName(this GetAttIntrinsic self, ITemplate template)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.AttributeName is IIntrinsic intrinsic
                       ? intrinsic.Evaluate(template).ToString()
                       : self.AttributeName.ToString();
        }

        /// <summary>
        /// Gets the value of the property on the target terraform resource referenced by this <c>!GetAtt</c>.
        /// </summary>
        /// <param name="self">Instance of intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="targetResource">The target resource.</param>
        /// <returns>An <see cref="IGetAttTargetEvaluation"/> containing result of the underlying resource visit.</returns>
        public static IGetAttTargetEvaluation GetTargetValue(this GetAttIntrinsic self, ITemplate template, StateFileResourceInstance targetResource)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            var context = new TerraformAttributeGetterContext(self.GetResolvedAttributeName(template));
            targetResource.Attributes.Accept(new TerraformAttributeGetterVisitor(), context);
            return context;
        }
    }
}