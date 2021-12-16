namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    internal class ResourceGroup
    {
        [YamlMember(Alias = "Group")]
        public string Name { get; set; }

        public List<ResourceTraits> Resources { get; set; }
    }
}