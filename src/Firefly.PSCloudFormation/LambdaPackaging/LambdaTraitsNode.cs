namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Node.JS lambda traits
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackaging.LambdaTraits" />
    internal class LambdaTraitsNode : LambdaTraits
    {
        /// <inheritdoc />
        public override Regex HandlerRegex =>
            new Regex(
                @"^\s*(module\.)?exports\.(?<handler>[\$\w]\w*)\s*=\s*(((async\s+)?function\s*\(\s*[\$\w]\w*\s*(,\s*[\$\w]\w*\s*){0,2}\))|(\(\s*[\$\w]\w*\s*(,\s*[\$\w]\w*\s*){0,2}\)\s*=\>))",
                RegexOptions.Multiline);

        /// <inheritdoc />
        public override string ScriptFileExtension => ".js";
    }
}