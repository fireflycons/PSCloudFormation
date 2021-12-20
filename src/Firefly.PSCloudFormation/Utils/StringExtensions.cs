namespace Firefly.PSCloudFormation.Utils
{
    using System.Linq;
    using System.Text.RegularExpressions;

    using Amazon.CloudFormation.Model.Internal.MarshallTransformations;

    internal static class StringExtensions
    {
        /// <summary>
        /// Convert camel case string to snake case.
        /// Handles the case where the first word is all caps.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>Converted string</returns>
        public static string CamelCaseToSnakeCase(this string self)
        {
            if (self == null)
            {
                return null;
            }

            var text = self;

            // If there is a string of upper case letters at the start, camel case it first (e.g. DNSName)
            var caps = new string(self.TakeWhile(char.IsUpper).ToArray());

            if (caps.Length > 2)
            {
                var r = new string(
                    caps.SelectMany(
                        (c, i) => i > 0 && i < caps.Length - 1
                                      ? new char[] { char.ToLowerInvariant(c) }
                                      : new char[] { c }).ToArray());

                text = text.Replace(caps, r);
            }

            return new string(text.SelectMany(
                (c, i) => i != 0 && char.IsUpper(c) && !char.IsUpper(text[i - 1])
                              ? new[] { '_', char.ToLowerInvariant(c) }
                              : new[] { char.ToLowerInvariant(c) }).ToArray());

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

        /// <summary>
        /// Quotes the specified self.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>Double quoted copy of the string.</returns>
        public static string Quote(this string self)
        {
            if (self == null)
            {
                return null;
            }

            if (self.StartsWith("\"") && self.EndsWith("\""))
            {
                return self;
            }

            return $"\"{self}\"";
        }
    }
}