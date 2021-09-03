namespace Firefly.PSCloudFormation.Terraform.Fixers
{
    using System.Linq;
    using System.Text.RegularExpressions;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.PlanDeserialization;

    /// <summary>
    /// Class containing methods for fixing various errors following a terraform plan
    /// </summary>
    internal static class PlanFixer
    {
        /// <summary>
        /// Fixes the specified script.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if the specified error was addressed; else <c>false</c></returns>
        public static bool Fix(HclScript script, PlanError error)
        {
            switch (error.ErrorType)
            {
                case PlanErrorType.MissingAttributeSeparator:

                    return FixMissingAttributeSeparator(script, error);

                case PlanErrorType.UnconfigurableAtribute:
                case PlanErrorType.InvalidOrUnknownKey:

                    return FixUnconfigurableAttribute(script, error);

                case PlanErrorType.MissingRequiredArgument:

                    var fixer = ResourceFixer.Create(script, error);

                    return fixer != null && fixer.Fix();

                default:

                    return false;
            }
        }

        /// <summary>
        /// Fixes missing attribute separator.
        /// So far found to be an unquoted JSON primitive containing punctuation
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if action was taken</returns>
        private static bool FixMissingAttributeSeparator(HclScript script, PlanError error)
        {
            var lineNumber = error.LineNumber;
            var rx = new Regex(@"^(\s*)([^""]([^\s]+)[^""])\s*");

            if (rx.IsMatch(script[lineNumber]))
            {
                script[lineNumber] = rx.Replace(
                    script[lineNumber],
                    m => $"{m.Groups[1].Value}\"{m.Groups[2].Value.Trim()}\" ");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fixes attributes that should not be in the HCL by removing them
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if action was taken</returns>
        private static bool FixUnconfigurableAttribute(HclScript script, PlanError error)
        {
            script.RemoveLines(
                Enumerable.Range(
                    error.Diagnostic.Range.Start.Line,
                    error.Diagnostic.Range.End.Line - error.Diagnostic.Range.Start.Line + 1));
            return true;
        }
    }
}