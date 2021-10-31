namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    internal class ResourceTraits
    {
        /// <summary>
        /// State attributes that should be omitted for all resources.
        /// </summary>
        private static readonly List<string> CommonUnconfigurableAttributes = new List<string> { "arn", "id", "create_date", "unique_id", "tags_all", "timeouts" };

        /// <summary>
        /// Map-style state attributes that should be omitted as key-value rather than block
        /// </summary>
        private static readonly List<string> CommonNonBlockTypeAttributes = new List<string> { "tags" };

        /// <summary>
        /// Gets the list of state attributes that should not be emitted as HCL
        /// </summary>
        /// <value>
        /// The ignored attributes.
        /// </value>
        public List<string> IgnoredAttributes =>
            CommonUnconfigurableAttributes.Concat(this.ResourceUnconfigurableAttributes).ToList();

        /// <summary>
        /// Gets the mapping style attributes that are not block definitions
        /// </summary>
        /// <value>
        /// The non block type attributes.
        /// </value>
        public List<string> NonBlockTypeAttributes =>
            CommonNonBlockTypeAttributes.Concat(this.ResourceNonBlockTypeAttributes).ToList();

        /// <summary>
        /// Gets default values for attributes that are <c>null</c> in the state file.
        /// </summary>
        public Dictionary<string, object> DefaultValues => this.ResourceDefaultValues;

        public List<string> RequiredAttributes => this.ResourceRequiredAttributes;

        /// <summary>
        /// Gets the resource specific attributes where terraform validate will error with <c>unconfigurable attribute</c>. 
        /// </summary>
        /// <value>
        /// The resource ignored attributes.
        /// </value>
        public virtual List<string> ResourceUnconfigurableAttributes { get; } = new List<string>();

        /// <summary>
        /// Gets the resource specific attributes where terraform validate will error with <c>Incorrect attribute value type</c>
        /// where an attribute is required, even if null or empty.
        /// </summary>
        public virtual List<string> ResourceRequiredAttributes { get; } = new List<string>();

        /// <summary>
        /// Gets the resource specific default values for <c>null</c> attributes in the state file.
        /// </summary>
        public virtual Dictionary<string, object> ResourceDefaultValues { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the mapping style attributes that are not block definitions
        /// </summary>
        /// <value>
        /// The non block type attributes.
        /// </value>
        public virtual List<string> ResourceNonBlockTypeAttributes { get; } = new List<string>();

        public static ResourceTraits GetTraits(string resourceType)
        {
            switch (resourceType)
            {

                case "aws_instance":

                    return new AwsInstanceTraits();

                case "aws_s3_bucket":

                    return new AwsS3BuckeTraits();

                case "aws_s3_bucket_policy":

                    return new AwsS3BucketPolicyTraits();

                case "aws_security_group":

                    return new AwsSecurityGroupTraits();

                default:

                    return new ResourceTraits();
            }
        }

        /// <summary>
        /// Determines wither a null or empty attribute should still be emitted.
        /// </summary>
        /// <param name="currentPath"></param>
        /// <returns><c>true</c> if attribute should be emitted; else <c>false</c></returns>
        public bool ShouldEmitAttribute(string currentPath)
        {
            return this.RequiredAttributes.Contains(currentPath) || this.DefaultValues.ContainsKey(currentPath);
        }

        public Scalar ApplyDefaultValue(string currentPath, Scalar scalar)
        {
            if (this.DefaultValues.ContainsKey(currentPath))
            {
                var newValue = this.DefaultValues[currentPath];
                return new Scalar(newValue, newValue is string);
            }

            return scalar;
        }
    }

    internal class AwsInstanceTraits : ResourceTraits
    {
        /// <inheritdoc />
        public override List<string> ResourceUnconfigurableAttributes => new List<string> { "instance_state", "primary_network_interface_id", "private_dns", "device_name", "volume_id" };
    }

    internal class AwsS3BuckeTraits : ResourceTraits
    {
        /// <inheritdoc />
        public override List<string> ResourceUnconfigurableAttributes => new List<string> { "bucket_domain_name", "bucket_regional_domain_name", "region", "private_dns", "device_name", "volume_id" };

        /// <inheritdoc />
        public override Dictionary<string, object> ResourceDefaultValues =>
            new Dictionary<string, object> { { "acl", "private" }, { "force_destroy", false } };
    }

    internal class AwsS3BucketPolicyTraits : ResourceTraits
    {
        /// <inheritdoc />
        public override List<string> ResourceRequiredAttributes => new List<string> { "policy" };
    }

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