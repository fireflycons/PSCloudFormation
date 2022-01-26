namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    using YamlDotNet.Serialization;

    /// <summary>
    /// Describes traits for the given resource.
    /// The state file contains much that doesn't directly translate to HCL, and this sorts that out.
    /// </summary>
    [DebuggerDisplay("{ResourceType}")]
    internal class ResourceTraits : IResourceTraits
    {
        /// <inheritdoc />
        public List<ConditionalAttribute> ConditionalAttributes { get; set; } = new List<ConditionalAttribute>();

        /// <inheritdoc />
        public List<string> MissingFromSchema { get; set; } = new List<string>();

        /// <inheritdoc />
        public Dictionary<string, string> AttributeMap { get; set; } = new Dictionary<string, string>();

        /// <inheritdoc />
        public List<List<string>> ConflictingArguments { get; set; } = new List<List<string>>();

        /// <inheritdoc />
        public Dictionary<string, object> DefaultValues { get; set; } = new Dictionary<string, object>();

        /// <inheritdoc />
        [YamlMember(Alias = "Resource")]
        public string ResourceType { get; set; }

        /// <inheritdoc />
        public Scalar ApplyDefaultValue(string currentPath, Scalar scalar)
        {
            return scalar;
        }
    }
}