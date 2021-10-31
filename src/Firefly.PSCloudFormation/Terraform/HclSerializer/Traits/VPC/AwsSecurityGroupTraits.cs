namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.VPC
{
    using System.Collections.Generic;

    /// <summary>
    /// Traits for <c>aws_security_group</c> resource.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.ResourceTraits" />
    internal class AwsSecurityGroupTraits : ResourceTraits
    {
        /// <inheritdoc />
        public override List<string> ResourceNonBlockTypeAttributes => new List<string> { "ingress", "egress" };

        /// <inheritdoc />
        public override List<string> ResourceUnconfigurableAttributes => new List<string> { "owner_id" };

        /// <inheritdoc />
        public override List<string> ResourceRequiredAttributes =>
            new List<string>
                {
                    "ingress.*.description",
                    "ingress.*.ipv6_cidr_blocks",
                    "ingress.*.prefix_list_ids",
                    "ingress.*.security_groups",
                    "egress.*.description",
                    "egress.*.ipv6_cidr_blocks",
                    "egress.*.prefix_list_ids",
                    "egress.*.security_groups"
                };
    }
}