using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Linq;
    using System.Text.RegularExpressions;

    internal static class DotHtmlFormatter
    {
        private static readonly Regex labelRegex = new Regex("label\\s*=\\s*([\\\"])(?<string>(?:\\\\\\1|.)*?)\\1", RegexOptions.Multiline);

        private static readonly Regex markupRegex = new Regex(@"\<[A-Z]+\>");

        public static string QuoteHtml(string dotGraph)
        {
            var mc = labelRegex.Matches(dotGraph);

            return mc.Cast<Match>().Select(match => match.Groups["string"].Value).Where(ContainsMarkup).Aggregate(
                dotGraph,
                (current, str) => current.Replace($"\"{str}\"", $"<{str}>"));
        }

        private static bool ContainsMarkup(string str)
        {
            return markupRegex.IsMatch(str);
        }
    }
}
