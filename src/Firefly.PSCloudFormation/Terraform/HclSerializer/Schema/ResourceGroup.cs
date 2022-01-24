namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    /// <summary>
    /// Groups a set of resources in <c>ResourceTraits.yaml</c>
    /// Serves no other purpose that to improve legibility of that file.
    /// </summary>
    internal class ResourceGroup
    {
        /// <summary>
        /// Gets or sets the group name (by service).
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [YamlMember(Alias = "Group")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the resources within the group.
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        public List<ResourceTraits> Resources { get; set; }
    }
}