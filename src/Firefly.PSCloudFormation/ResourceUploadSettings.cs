namespace Firefly.PSCloudFormation
{
    using System.Collections;
    using System.IO;

    /// <summary>
    /// Describes a packager resource to upload to S3
    /// </summary>
    internal class ResourceUploadSettings
    {
        /// <summary>
        /// Gets or sets the file that may be uploaded.
        /// </summary>
        /// <value>
        /// The file.
        /// </value>
        public FileInfo File { get; set; }

        /// <summary>
        /// Gets or sets the hash computed for the file content
        /// </summary>
        /// <value>
        /// The hash.
        /// </value>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the S3 key prefix.
        /// </summary>
        /// <value>
        /// The key prefix.
        /// </value>
        public string KeyPrefix { get; set; }

        /// <summary>
        /// Gets or sets the user metadata to associate with the uploaded object.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        public IDictionary Metadata { get; set; }

        /// <summary>
        /// Gets or sets the S3 artifact representing the uploaded object
        /// </summary>
        /// <value>
        /// The s3 key.
        /// </value>
        public S3Artifact S3Artifact { get; set; }
    }
}