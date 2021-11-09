namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System;
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
        public List<List<string>> ConflictingArguments { get; set; } = new List<List<string>>();

        /// <inheritdoc />
        public Dictionary<string, object> DefaultValues { get; set; } = new Dictionary<string, object>();

        /// <inheritdoc />
        public List<string> NonBlockTypeAttributes { get; set; } = new List<string>();

        /// <inheritdoc />
        public List<string> RequiredAttributes { get; set; } = new List<string>();

        [YamlMember(Alias = "Resource")]
        public string ResourceType { get; set; }

        /// <inheritdoc />
        public List<string> UnconfigurableAttributes { get; set; } = new List<string>();

        /// <summary>
        /// Gets traits class for current resource type.
        /// </summary>
        /// <param name="resourceType">Type of the resource being serialized.</param>
        /// <returns>Resource specific <see cref="ResourceTraits"/></returns>
        public static ResourceTraits GetTraits(string resourceType)
        {
            switch (resourceType)
            {
                default:

                    return new ResourceTraits();
            }
        }

        /// <inheritdoc />
        public Scalar ApplyDefaultValue(string currentPath, Scalar scalar)
        {
            return scalar;
        }

        /// <inheritdoc />
        public bool ShouldEmitAttribute(string currentPath)
        {
            return this.RequiredAttributes.Contains(currentPath) || this.DefaultValues.ContainsKey(currentPath);
        }

        /// <inheritdoc />
        public bool IsConflictingArgument(string currentPath)
        {
            return false;
        }
    }
}