namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    using YamlDotNet.Serialization;

    /// <summary>
    /// Describes traits for the given resource.
    /// The state file contains much that doesn't directly translate to HCL, and this sorts that out.
    /// </summary>
    internal class ResourceTraits : IResourceTraits
    {
        /// <inheritdoc />
        public List<string> BlockObjectAttributes { get; set; } = new List<string>();

        /// <inheritdoc />
        public List<ConditionalAttribute> ConditionalAttributes { get; set; } = new List<ConditionalAttribute>();

        /// <inheritdoc />
        public Dictionary<string, string> AttributeMap { get; set; } = new Dictionary<string, string>();

        /// <inheritdoc />
        public List<string> ComputedAttributes { get; set; } = new List<string>();

        /// <inheritdoc />
        /// <inheritdoc />
        public List<List<string>> ConflictingArguments { get; set; } = new List<List<string>>();

        /// <inheritdoc />
        public Dictionary<string, object> DefaultValues { get; set; } = new Dictionary<string, object>();

        /// <inheritdoc />
        public List<string> NonBlockTypeAttributes { get; set; } = new List<string>();

        /// <inheritdoc />
        public List<string> RequiredAttributes { get; set; } = new List<string>();

        /// <inheritdoc />
        [YamlMember(Alias = "Resource")]
        public string ResourceType { get; set; }

        /// <inheritdoc />
        public Scalar ApplyDefaultValue(string currentPath, Scalar scalar)
        {
            return scalar;
        }

        /// <inheritdoc />
        public bool IsConflictingArgument(string currentPath)
        {
            return false;
        }
    }
}