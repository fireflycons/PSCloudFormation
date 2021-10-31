namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.EC2
{
    using System.Collections.Generic;

    /// <summary>
    /// Traits for <c>aws_instance</c> resource.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.ResourceTraits" />
    internal class AwsInstanceTraits : ResourceTraits
    {
        /// <inheritdoc />
        public override List<string> ResourceUnconfigurableAttributes => new List<string> { "instance_state", "primary_network_interface_id", "private_dns", "device_name", "volume_id" };
    }
}