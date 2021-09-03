namespace Firefly.PSCloudFormation.Utils
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions for lists
    /// </summary>
    internal static class ListExtensions
    {
        /// <summary>
        /// Determines whether the specified list contains the specified text.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if the specified list contains specified text; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsText(this List<string> list, string text)
        {
            return list != null && list.Any(line => line.Contains(text));
        }
    }
}