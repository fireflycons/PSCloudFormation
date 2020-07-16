namespace Firefly.PSCloudFormation
{
    using System;

    using Amazon;
    using Amazon.Runtime;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.S3;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Concrete implementation of <see cref="ICloudFormationContext"/> for PowerShell
    /// </summary>
    /// <seealso cref="Firefly.CloudFormation.ICloudFormationContext" />
    // ReSharper disable once InconsistentNaming
    public class PSCloudFormationContext : ICloudFormationContext
    {
        /// <summary>
        /// Gets or sets the AWS account identifier.
        /// </summary>
        /// <value>
        /// The account identifier.
        /// </value>
        public string AccountId { get; set; }

        /// <summary>
        /// Gets or sets the cloud formation endpoint URL.
        /// </summary>
        /// <value>
        /// The cloud formation endpoint URL.
        /// </value>
        public Uri CloudFormationEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        /// <value>
        /// The credentials.
        /// </value>
        public AWSCredentials Credentials { get; set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <value>
        /// The region.
        /// </value>
        public RegionEndpoint Region { get; set; }

        /// <summary>
        /// Gets or sets the s3 endpoint URL.
        /// </summary>
        /// <value>
        /// The s3 endpoint URL.
        /// </value>
        public Uri S3EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the STS endpoint URL.
        /// </summary>
        /// <value>
        /// The STS endpoint URL.
        /// </value>
        public Uri STSEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the timestamp generator.
        /// </summary>
        /// <value>
        /// The timestamp generator.
        /// </value>
        public ITimestampGenerator TimestampGenerator { get; set; }

        /// <summary>
        /// Gets or sets the S3 utility.
        /// Used for uploading oversize objects to S3
        /// </summary>
        /// <value>
        /// The S3 utility.
        /// </value>
        public IS3Util S3Util { get; set; }
    }
}