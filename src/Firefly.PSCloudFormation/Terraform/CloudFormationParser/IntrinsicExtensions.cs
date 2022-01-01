namespace Firefly.PSCloudFormation.Terraform.CloudFormationParser
{
    using System;

    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.PSCloudFormation.Terraform.DependencyResolver;

    /// <summary>
    /// Other extension methods for all intrinsic functions.
    /// </summary>
    internal static class IntrinsicExtensions
    {
        /// <summary>
        /// Gets the <see cref="IntrinsicInfo"/> attached to the ExtraData member during visit to the parsed CloudFormation template.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>The attached <see cref="IntrinsicInfo"/></returns>
        /// <exception cref="ArgumentNullException">Intrinsic cannot be null</exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot call GetInfo before running Dependency Resolver (which sets this)
        /// - or -
        /// Unexpected type on the ExtraData property of the intrinsic.
        /// </exception>
        public static IntrinsicInfo GetInfo(this IIntrinsic self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            switch (self.ExtraData)
            {
                case null:

                    throw new InvalidOperationException(
                        $"{self.TagName}: Cannot call GetInfo before running Dependency Resolver");

                case IntrinsicInfo info:

                    return info;

                default:

                    throw new InvalidOperationException(
                        $"{self.TagName}: Unexpected type {self.ExtraData.GetType().FullName} in ExtraData member.");
            }
        }
    }
}