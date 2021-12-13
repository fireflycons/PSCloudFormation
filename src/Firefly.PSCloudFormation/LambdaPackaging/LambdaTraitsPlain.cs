namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Plain (non-script) lambda traits - there are none as no special processing done.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackaging.LambdaTraits" />
    internal class LambdaTraitsPlain : LambdaTraits
    {
        /// <inheritdoc />
        public override Regex HandlerRegex => null;

        /// <inheritdoc />
        public override string ScriptFileExtension => string.Empty;
    }
}