namespace Firefly.PSCloudFormation
{
    /// <summary>
    /// Result of uploading a packaged artifact
    /// </summary>
    internal class S3Artifact
    {
        /// <summary>
        /// Gets or sets the name of the bucket.
        /// </summary>
        /// <value>
        /// The name of the bucket.
        /// </value>
        public string BucketName { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets the S3 URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string Url => $"https://{this.BucketName}.s3.amazonaws.com/{this.Key}";
    }
}