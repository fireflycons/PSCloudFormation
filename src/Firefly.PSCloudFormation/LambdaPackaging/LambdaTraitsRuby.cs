namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Ruby lambda traits
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackaging.LambdaTraits" />
    internal class LambdaTraitsRuby : LambdaTraits
    {
        /// <inheritdoc />
        public override Regex HandlerRegex =>
            new Regex(
                @"^\s*def\s+(?<handler>[^\d\W]\w*)\s*\(\s*[^\d\W]\w*:\s*,\s*[^\d\W]\w*:\s*\)\s*",
                RegexOptions.Multiline);

        /// <inheritdoc />
        public override string ScriptFileExtension => ".rb";
    }
}