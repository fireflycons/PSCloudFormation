namespace Firefly.CloudFormation
{
    using System;

    using Amazon;
    using Amazon.Runtime;

    using Firefly.CloudFormation.S3;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Describes the context
    /// </summary>
    public interface ICloudFormationContext
    {
        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        /// <value>
        /// The credentials.
        /// </value>
        AWSCredentials Credentials { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <value>
        /// The region.
        /// </value>
        RegionEndpoint Region { get; set; }

        /// <summary>
        /// Gets or sets the cloud formation endpoint URL.
        /// </summary>
        /// <value>
        /// The cloud formation endpoint URL.
        /// </value>
        Uri CloudFormationEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the s3 endpoint URL.
        /// </summary>
        /// <value>
        /// The s3 endpoint URL.
        /// </value>
        Uri S3EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the STS endpoint URL.
        /// </summary>
        /// <value>
        /// The STS endpoint URL.
        /// </value>
        // ReSharper disable once InconsistentNaming
        Uri STSEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the AWS account identifier.
        /// </summary>
        /// <value>
        /// The account identifier.
        /// </value>
        string AccountId { get; set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the timestamp generator.
        /// </summary>
        /// <value>
        /// The timestamp generator.
        /// </value>
        ITimestampGenerator TimestampGenerator { get; set; }

        /// <summary>
        /// Gets or sets the S3 utility.
        /// Used for uploading oversize objects to S3
        /// </summary>
        /// <value>
        /// The S3 utility.
        /// </value>
        IS3Util S3Util { get; set; }
    }
}