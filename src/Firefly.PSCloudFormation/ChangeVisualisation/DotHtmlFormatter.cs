namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Temporary fix for lack of markup support in <c>QuikGraph</c>
    /// <see href="https://github.com/KeRNeLith/QuikGraph/issues/27"/>
    /// </summary>
    internal static class DotHtmlFormatter
    {
        /// <summary>
        /// Match a DOT label
        /// </summary>
        private static readonly Regex LabelRegex = new Regex(
            "label\\s*=\\s*([\\\"])(?<string>(?:\\\\\\1|.)*?)\\1",
            RegexOptions.Multiline);

        /// <summary>
        /// Match markup in a label
        /// </summary>
        private static readonly Regex MarkupRegex = new Regex(@"\<[A-Z]+\>");

        /// <summary>
        /// Re-quote markup string with angle brackets
        /// </summary>
        /// <param name="dotGraph">The dot graph.</param>
        /// <returns>DOT graph with any markup appropriately quoted</returns>
        public static string QuoteHtml(string dotGraph)
        {
            var mc = LabelRegex.Matches(dotGraph);

            return mc.Cast<Match>().Select(match => match.Groups["string"].Value).Where(ContainsMarkup).Aggregate(
                dotGraph,
                (current, str) => current.Replace($"\"{str}\"", $"<{str}>"));
        }


        /// <summary>
        /// Determines whether the specified string contains markup.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>
        ///   <c>true</c> if the specified string contains markup; otherwise, <c>false</c>.
        /// </returns>
        private static bool ContainsMarkup(string str)
        {
            return MarkupRegex.IsMatch(str);
        }
    }
}