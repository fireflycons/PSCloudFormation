using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation
{
    using Amazon.S3;
    using Amazon.SecurityToken;

    using Firefly.CloudFormation.Utils;

    public interface IPSAwsClientFactory : IAwsClientFactory, IDisposable
    {
        /// <summary>
        /// <para>
        /// Creates an s3 client.
        /// </para>
        /// <para>
        /// This is used to put templates that are larger then 51,200 bytes up to S3 prior to running the CloudFormation change. The following permissions are required
        /// <list type="bullet">
        /// <item>
        /// <term>s3:CreateBucket</term>
        /// <description>To create the bucket used by this library if it does not exist.</description>
        /// </item>
        /// <item>
        /// <term>s3:PutLifecycleConfiguration</term>
        /// <description>Used when creating bucket to prevent buildup of old templates.</description>
        /// </item>
        /// <item>
        /// <term>s3:PutObject</term>
        /// <description>To upload templates to the bucket.</description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <returns>An S3 client.</returns>
        IAmazonS3 CreateS3Client();

        /// <summary>
        /// <para>
        /// Creates an STS client.
        /// </para>
        /// <para>
        /// Principally used to determine the AWS account ID to ensure uniqueness of S3 bucket name. The following permissions are required
        /// <list type="bullet">
        /// <item>
        /// <term>sts:GetCallerIdentity</term>
        /// <description>To get the account ID for the caller's account.</description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <returns>A Security Token Service client.</returns>
        // ReSharper disable once StyleCop.SA1650
        // ReSharper disable once InconsistentNaming
        IAmazonSecurityTokenService CreateSTSClient();
    }
}
