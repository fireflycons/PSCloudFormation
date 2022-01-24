namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
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
        /// Determines whether argument at current path should be omitted due to its value.
        /// </summary>
        /// <param name="self"><see cref="IResourceTraits"/> derivative</param>
        /// <param name="currentPath">The current path.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if argument at current path should be omitted due to its value, else <c>false</c>.
        /// </returns>
        public static bool IsOmittedConditionalAttrbute(this IResourceTraits self, string currentPath, string value)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            var conditionalAttribute = self.ConditionalAttributes.FirstOrDefault(ca => ca.Name.IsLike(currentPath));

            if (conditionalAttribute == null)
            {
                return false;
            }

            return value == conditionalAttribute.Value;
        }
    }
}