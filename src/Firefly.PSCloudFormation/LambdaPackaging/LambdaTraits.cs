namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System;
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

        /// <summary>
        /// Create a traits class from the lambda runtime identifier.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Unsupported lambda runtime '{runtime}'. Only script types allowed here.</exception>
        public static LambdaTraits FromRuntime(string runtime)
        {
            if (runtime.StartsWith("python"))
            {
                return new LambdaTraitsPython();
            }

            if (runtime.StartsWith("nodejs"))
            {
                return new LambdaTraitsNode();
            }

            if (runtime.StartsWith("ruby"))
            {
                return new LambdaTraitsRuby();
            }

            throw new InvalidOperationException(
                $"Unsupported lambda runtime '{runtime}'. Only script types allowed here.");
        }
    }
}