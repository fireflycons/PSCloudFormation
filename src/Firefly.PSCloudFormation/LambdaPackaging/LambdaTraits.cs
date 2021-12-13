namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Traits for the various lambda types
    /// </summary>
    internal abstract class LambdaTraits
    {
        /// <summary>
        /// Gets regex for parsing out the handler function.
        /// </summary>
        /// <value>
        /// The handler regex.
        /// </value>
        public abstract Regex HandlerRegex { get; }

        /// <summary>
        /// Gets the script file extension.
        /// </summary>
        /// <value>
        /// The script file extension.
        /// </value>
        public abstract string ScriptFileExtension { get; }
    }
}