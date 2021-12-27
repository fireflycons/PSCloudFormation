namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;

    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform.DependencyResolver;

    /// <summary>
    /// Attach an extra Evaluate method to intrinsics which works on actual values from the state file.
    /// </summary>
    // ReSharper disable once StyleCop.SA1650 - Extension methods
    // ReSharper disable once UnusedMember.Global
    internal static class IntrinsicEvaluationExtensions
    {
        /// <summary>
        /// Evaluates the specified intrinsic.
        /// </summary>
        /// <param name="self">The intrinsic.</param>
        /// <param name="intrinsicInfo">The <see cref="IntrinsicInfo"/> related to this intrinsic.</param>
        /// <returns>The evaluated result.</returns>
        public static object Evaluate(this IIntrinsic self, IntrinsicInfo intrinsicInfo)
        {
            switch (self.Type)
            {
                case IntrinsicType.Join:

                    return EvaluateJoin((JoinIntrinsic)self);

                default:

                    return intrinsicInfo.InitialEvaluation;
            }
        }

        /// <summary>
        /// Evaluates a join intrinsic.
        /// </summary>
        /// <param name="joinIntrinsic">The join intrinsic.</param>
        /// <returns>Evaluation of the join.</returns>
        private static object EvaluateJoin(JoinIntrinsic joinIntrinsic)
        {
            var elements = new List<string>();

            foreach (var item in joinIntrinsic.Items)
            {
                if (item is IIntrinsic intrinsic)
                {
                    var nested = (IntrinsicInfo)intrinsic.ExtraData;
                    
                    // ReSharper disable once PossibleNullReferenceException - if null, then it's most likely a bug.
                    elements.Add(nested.Intrinsic.Evaluate(nested).ToString());
                }
                else
                {
                    elements.Add(item.ToString());
                }
            }

            return string.Join(joinIntrinsic.Separator, elements);
        }
    }
}