namespace Firefly.PSCloudFormation.Terraform.Fixers
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.PlanDeserialization;

    /// <summary>
    /// Base class for specific resource fixers
    /// </summary>
    internal abstract class ResourceFixer
    {
        /// <summary>
        /// Regex to extract resource type from error diagnostic
        /// </summary>
        private static readonly Regex ResourceTypeRegex = new Regex(@"^resource\s+""(?<type>\w+)""");

        /// <summary>
        /// Map of resource type to fixer type
        /// </summary>
        private static readonly Dictionary<string, Type> ResourceFixers =
            new Dictionary<string, Type> { { "aws_dynamodb_table", typeof(DynamodbTableFixer) } };

        /// <summary>
        /// Creates a resource fixer for the given error.
        /// </summary>
        /// <param name="script">The script to fix.</param>
        /// <param name="error">The error to resolve.</param>
        /// <returns>A <see cref="ResourceFixer"/> for the given error; else <c>null</c> if one not available</returns>
        public static ResourceFixer Create(HclScript script, PlanError error)
        {
            var m = ResourceTypeRegex.Match(error.Diagnostic.Snippet.Context);

            if (!m.Success)
            {
                return null;
            }

            var type = m.Groups["type"].Value;

            if (!ResourceFixers.ContainsKey(type))
            {
                return null;
            }

            return (ResourceFixer)Activator.CreateInstance(ResourceFixers[type], script, error);
        }

        /// <summary>
        /// Fix this error.
        /// </summary>
        /// <returns><c>true</c> if error was resolved; else <c>false</c></returns>
        public abstract bool Fix();
    }
}