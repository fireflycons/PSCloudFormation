namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.S3
{
    using System.Collections.Generic;

    /// <summary>
    /// Traits for <c>aws_s3_bucket_policy</c> resource.
    /// </summary>
    internal class AwsS3BucketPolicyTraits : ResourceTraits
    {
        /// <inheritdoc />
        public override List<string> ResourceRequiredAttributes => new List<string> { "policy" };
    }
}