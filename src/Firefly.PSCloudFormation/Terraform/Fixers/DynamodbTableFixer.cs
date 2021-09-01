namespace Firefly.PSCloudFormation.Terraform.Fixers
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.PlanDeserialization;

    /// <summary>
    /// Fixer for Dynamo tables
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Fixers.ResourceFixer" />
    internal class DynamodbTableFixer : ResourceFixer
    {
        /// <summary>
        /// The script
        /// </summary>
        private readonly HclScript script;

        /// <summary>
        /// The error
        /// </summary>
        private readonly PlanError error;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamodbTableFixer"/> class.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="error">The error.</param>
        public DynamodbTableFixer(HclScript script, PlanError error)
        {
            this.error = error;
            this.script = script;
        }

        /// <summary>
        /// Fix this error.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if error was resolved; else <c>false</c>
        /// </returns>
        public override bool Fix()
        {
            if (this.error.ErrorType != PlanErrorType.MissingRequiredArgument)
            {
                return false;
            }

            if (Regex.IsMatch(this.error.Diagnostic.Snippet.Code, @"^\s*ttl\s+\{"))
            {
                return this.FixTTLMissingAttribute();
            }

            return false;
        }

        /// <summary>
        /// Fixes the TTL missing attribute by removing the TTL block.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if error was resolved; else <c>false</c>
        /// </returns>
        // ReSharper disable once InconsistentNaming
        private bool FixTTLMissingAttribute()
        {
            // Remove the entire TTL block.
            // Cannot define TTL without an attribute, even when false
            var line = this.error.LineNumber;
            var linesToRemove = new List<int> { line };

            do
            {
                linesToRemove.Add(++line);
            }
            while (!Regex.IsMatch(this.script[line], @"^\s*\}\s*$"));

            this.script.RemoveLines(linesToRemove);
            return true;
        }
    }
}