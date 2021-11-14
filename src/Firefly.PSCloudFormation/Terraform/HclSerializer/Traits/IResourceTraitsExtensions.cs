namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System;
    using System.Linq;

    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Extension methods for <see cref="IResourceTraits"/>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal static class IResourceTraitsExtensions
    {
        /// <summary>
        /// Determine from the resource traits whether a given attribute should be emitted.
        /// </summary>
        /// <param name="self"><see cref="IResourceTraits"/> derivative</param>
        /// <param name="currentPath">The current path.</param>
        /// <param name="analysis">The analysis.</param>
        /// <returns><c>true</c> if the attribute should be emitted; else <c>false</c> to silently consume it.</returns>
        public static bool ShouldEmitAttribute(this IResourceTraits self, string currentPath, AttributeContent analysis)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            if (self.RequiredAttributes.Any(currentPath.IsLike))
            {
                // Required attributes overrides unconfigurable attributes
                return true;
            }

            if (self.IsConflictingArgument(currentPath) || self.UnconfigurableAttributes.Any(currentPath.IsLike))
            {
                // Even when it has a value
                return false;
            }

            return analysis == AttributeContent.HasValue;
        }
    }
}