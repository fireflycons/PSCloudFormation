﻿namespace Firefly.CloudFormation.CloudFormation
{
    using System;

    /// <summary>
    /// String extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// The quotes
        /// </summary>
        private static readonly string[] Quotes = { "'", "\"" };

        /// <summary>
        /// Remove quotes from string if present
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>Unquoted string</returns>
        /// <exception cref="ArgumentException">String is null - self</exception>
        public static string Unquote(this string self)
        {
            if (self == null)
            {
                throw new ArgumentException("String is null", nameof(self));
            }

            var temp = self.Trim();

            foreach (var q in Quotes)
            {
                if (temp.StartsWith(q) && temp.EndsWith(q))
                {
                    return temp.Substring(1, temp.Length - 2);
                }
            }

            return temp;
        }
    }
}