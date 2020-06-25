namespace Firefly.CloudFormation.S3
{
    using System;

    /// <summary>
    /// Describes the bucket used to manage oversize (> 51200 bytes) templates
    /// </summary>
    internal class CloudFormationBucket
    {
        /// <summary>
        /// Gets or sets the name of the bucket.
        /// </summary>
        /// <value>
        /// The name of the bucket.
        /// </value>
        public string BucketName { get; set; }

        /// <summary>
        /// Gets or sets the bucket URI.
        /// </summary>
        /// <value>
        /// The bucket URI.
        /// </value>
        public Uri BucketUri { get; set; }
    }
}