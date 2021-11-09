namespace Firefly.PSCloudFormation.Utils
{
    using System.Linq;

    internal static class StringExtensions
    {
        /// <summary>
        /// Convert camel case string to snake case.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>Converted string</returns>
        public static string CamelCaseToSnakeCase(this string self)
        {
            if (self == null)
            {
                return null;
            }

            var result = self.SelectMany(
                (c, i) => i != 0 && char.IsUpper(c) && !char.IsUpper(self[i - 1])
                              ? new char[] { '_', char.ToLowerInvariant(c) }
                              : new char[] { char.ToLowerInvariant(c) });

            return new string(result.ToArray());
        }
    }
}