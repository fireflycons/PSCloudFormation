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
                // Required attributes overrides computed attributes attributes
                return true;
            }

            if (self.IsConflictingArgument(currentPath) || self.ComputedAttributes.Any(currentPath.IsLike))
            {
                // Even when it has a value
                return false;
            }

            return new[]
                       {
                           AttributeContent.BlockList, AttributeContent.BlockObject, AttributeContent.Sequence,
                           AttributeContent.Mapping, AttributeContent.Value
                       }.Contains(analysis);
        }

        /// <summary>
        /// Determines if argument at current path is not a block, therefore should be emitted as <c>arg = { ... </c>, e.g. SG ingress rules, tags
        /// </summary>
        /// <param name="self"><see cref="IResourceTraits"/> derivative</param>
        /// <param name="currentPath">The current path.</param>
        /// <returns>
        ///   <c>true</c> if argument at current path is not a block otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNonBlockAttribute(this IResourceTraits self, string currentPath)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.NonBlockTypeAttributes.Any(currentPath.IsLike);
        }

        /// <summary>
        /// Determines if argument at current path is a block object, meaning that it does not contain a sequence, e.g. timeouts
        /// </summary>
        /// <param name="self"><see cref="IResourceTraits"/> derivative</param>
        /// <param name="currentPath">The current path.</param>
        /// <returns>
        ///   <c>true</c> if argument at current path is a block object otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBlockObject(this IResourceTraits self, string currentPath)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.BlockObjectAttributes.Any(currentPath.IsLike);
        }
    }
}