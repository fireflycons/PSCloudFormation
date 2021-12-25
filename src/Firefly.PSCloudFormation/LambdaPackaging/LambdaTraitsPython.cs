﻿namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Python lambda traits
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackaging.LambdaTraits" />
    internal class LambdaTraitsPython : LambdaTraits
    {
        /// <inheritdoc />
        public override Regex HandlerRegex =>
            new Regex(
                @"^\s*def\s+(?<handler>[^\d\W]\w*)\s*\(\s*[^\d\W]\w*\s*(:\s*\w+\s*)?,\s*[^\d\W]\w*\s*(:\s*\w+\s*)?(,\s*(([^\d\W]\w*\s*=\s*\w*)|(\**\w+))\s*)*\)\s*(\-\>\s*\w+\s*)?:",
                RegexOptions.Multiline);

        /// <inheritdoc />
        public override string ScriptFileExtension => ".py";
    }
}