namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.S3
{
    using System.Collections.Generic;

    /// Traits for <c>aws_s3_bucket</c> resource.
    internal class AwsS3BucketTraits : ResourceTraits
    {
        /// <inheritdoc />
        public override List<string> ResourceUnconfigurableAttributes => new List<string> { "bucket_domain_name", "bucket_regional_domain_name", "region", "private_dns", "device_name", "volume_id" };

        /// <inheritdoc />
        public override Dictionary<string, object> ResourceDefaultValues =>
            new Dictionary<string, object> { { "acl", "private" }, { "force_destroy", false } };
    }
}