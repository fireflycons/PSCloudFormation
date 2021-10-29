namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    internal class HclRepresentaion
    {
        [YamlMember(Alias = "format_version")]
        public string FormatVersion { get; set; }

        [YamlIgnore]
        public List<HclResourceRepresentation> Resources => this.values.root_module.Resources;

        [YamlMember(Alias = "terraform_version")]
        public string TerraformVersion { get; set; }

        [YamlMember(Alias = "values")]
        public Values values { get; set; }

        internal class RootModule
        {
            [YamlMember(Alias = "resources")]
            public List<HclResourceRepresentation> Resources { get; set; }
        }

        internal class Values
        {
            public RootModule root_module { get; set; }
        }
    }
}