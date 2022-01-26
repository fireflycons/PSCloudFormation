namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents path to an attribute in resource schema
    /// </summary>
    /// <seealso cref="string" />
    [DebuggerDisplay("{CurrentPath}")]
    internal class AttributePath
    {
        /// <summary>
        /// Regex to locate index expression in JSON path
        /// </summary>
        private static readonly Regex IndexerRegex = new Regex(@"\[\d+\]");

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributePath"/> class.
        /// </summary>
        /// <param name="jsonPath">The JSON path to convert.</param>
        public AttributePath(string jsonPath)
        {
            this.CurrentPath = IndexerRegex.Replace(jsonPath, ".0");
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="AttributePath"/> class from being created.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private AttributePath()
        {
        }

        /// <summary>
        /// Gets the current path.
        /// </summary>
        /// <value>
        /// The current path.
        /// </value>
        public string CurrentPath { get; } = string.Empty;

        /// <summary>
        /// Splits the specified path on JSON path semantics.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Split path</returns>
        public static string[] Split(string path)
        {
            if (!path.Contains("["))
            {
                return path.Split('.');
            }

            // JSON path will place a path component which itself contains periods into dictionary syntax
            var split = new List<string>();
            var inBracket = false;
            var sb = new StringBuilder();

            foreach (var c in path)
            {
                switch (c)
                {
                    case '\'':

                        break;

                    case '[':

                        inBracket = true;
                        split.Add(sb.ToString());
                        sb.Clear();
                        break;

                    case ']':

                        inBracket = false;
                        split.Add(sb.ToString());
                        sb.Clear();
                        break;

                    case '.' when !inBracket:

                        split.Add(sb.ToString());
                        sb.Clear();
                        break;

                    default:

                        sb.Append(c);
                        break;
                }
            }

            if (sb.Length > 0)
            {
                split.Add(sb.ToString());
            }

            return split.ToArray();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="AttributePath"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(AttributePath p)
        {
            return p.CurrentPath;
        }
    }
}