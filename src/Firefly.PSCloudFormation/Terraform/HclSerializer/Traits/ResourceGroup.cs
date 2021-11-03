namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Firefly.EmbeddedResourceLoader;

    using YamlDotNet.Serialization;

    internal class ResourceGroup
    {
        
        [YamlMember(Alias = "Group")]
        public string Name { get; set; }

        public List<ResourceTraits> Resources { get; set; }
    }
}