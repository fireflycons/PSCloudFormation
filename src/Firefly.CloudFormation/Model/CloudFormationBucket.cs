namespace Firefly.CloudFormation.Model
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

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CloudFormationBucket"/> is initialized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if initialized; otherwise, <c>false</c>.
        /// </value>
        public bool Initialized { get; set; }
    }
}