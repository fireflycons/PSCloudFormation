namespace Firefly.PSCloudFormation.Utils
{
    using System.Linq;
    using System.Text.RegularExpressions;

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

        /// <summary>
        /// Perform a string wildcard match as in PowerShell's <c>-like</c> operator..
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="pattern">The pattern.</param>
        /// <returns><c>true</c> if match; else <c>false</c></returns>
        public static bool IsLike(this string self, string pattern)
        {
            if (self == null)
            {
                return false;
            }

            if (!pattern.Contains("*"))
            {
                return self == pattern;
            }

            var rx = new Regex(pattern.Replace(".", @"\.").Replace("*", ".*"));

            return rx.IsMatch(self);
        }
    }
}