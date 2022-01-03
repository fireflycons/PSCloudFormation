namespace Firefly.PSCloudFormation.Utils
{
    using System;
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

        /// <summary>
        /// Flattens the given <c>IEnumerable{T}</c> with the use of a function that maps a <c>T</c> to an  <c>IEnumerable{T}</c> describing the parent -> children relation of your data.
        /// </summary>
        /// <typeparam name="T">Type of the object within the container.</typeparam>
        /// <param name="self">Enumeration to flatten.</param>
        /// <param name="f">function that maps a <c>T</c> to an <c>IEnumerable{T}</c> describing the parent -> children relation.</param>
        /// <returns>Depth-first flattened enumeration of graph of <c>T</c>.</returns>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> self, Func<T, IEnumerable<T>> f) => self.SelectMany(i => new [] { i }.Concat(f(i).Flatten(f)));
    }
}