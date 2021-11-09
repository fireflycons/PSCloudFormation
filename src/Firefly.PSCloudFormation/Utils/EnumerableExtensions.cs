namespace Firefly.PSCloudFormation.Utils
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions for IEnumerable
    /// </summary>
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Produces an enumeration of tuples, each tuple containing the list index and the object.
        /// </summary>
        /// <typeparam name="T">Type of the object within the container.</typeparam>
        /// <param name="source">The source enumeration.</param>
        /// <returns>Enumeration of tuples, each tuple containing the list index and the object.</returns>
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source == null ? new List<(T item, int index)>() : source.Select((item, index) => (item, index));
        }
    }
}