namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    internal class HclResourceRepresentation
    {
        [YamlMember(Alias = "address")]
        public string Address { get; set; }

        [YamlMember(Alias = "mode")]
        public string Mode { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "provider_name")]
        public string ProviderName { get; set; }

        [YamlMember(Alias = "schema_version")]
        public int SchemaVersion { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "values")]
        public Dictionary<string, object> Values { get; set; }
    }
}