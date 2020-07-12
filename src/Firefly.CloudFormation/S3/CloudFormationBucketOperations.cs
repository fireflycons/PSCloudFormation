namespace Firefly.CloudFormation.S3
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.SecurityToken.Model;

    using Firefly.CloudFormation.CloudFormation;
    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// <para>
    /// Manages operations on the bucket in which oversize templates are uploaded to
    /// </para>
    /// <para>
    /// If instantiated using the constructor that does not take a bucket name which is the default for <see cref="CloudFormationRunner"/>
    /// then the library will use its own private bucket, creating it as needed. The generated bucket name includes the account number
    /// and region of the caller's profile to ensure DNS uniqueness. Buckets created in this manner will have a 7 day delete lifecycle
    /// rule included to prevent buildup of temporary objects such as templates over 51,200 bytes that cannot be sent as text via the APIs.
    /// </para>
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class CloudFormationBucketOperations
    {
        /// <summary>
        /// The client factory
        /// </summary>
        private readonly IAwsClientFactory clientFactory;

        /// <summary>
        /// The log
        /// </summary>
        private readonly ICloudFormationContext context;

        /// <summary>
        /// The timestamp generator. Unit tests supply an alternate so object names may be validated.
        /// </summary>
        private readonly ITimestampGenerator timestampGenerator;

        /// <summary>
        /// The cloud formation bucket
        /// </summary>
        private readonly CloudFormationBucket cloudFormationBucket;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationBucketOperations"/> class using the
        /// library's private bucket which will be created if it doesn't exist and a 7 day lifecycle rule
        /// applied to prevent build-up of temporary objects.
        /// </summary>
        /// <param name="clientFactory">The AWS client factory.</param>
        /// <param name="context">The context.</param>
        public CloudFormationBucketOperations(IAwsClientFactory clientFactory, ICloudFormationContext context)
        {
            this.context = context;
            this.clientFactory = clientFactory;
            this.timestampGenerator = context.TimestampGenerator ?? new TimestampGenerator();

            using (var sts = this.clientFactory.CreateSTSClient())
            {
                var account = sts.GetCallerIdentityAsync(new GetCallerIdentityRequest()).Result.Account;
                var bucketName = $"cf-templates-pscloudformation-{this.context.Region.SystemName}-{account}";

                this.cloudFormationBucket = new CloudFormationBucket
                                                {
                                                    BucketName = bucketName,
                                                    BucketUri = new Uri($"https://{bucketName}.s3.amazonaws.com"),
                                                    Initialized = false
                                                };
            }

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationBucketOperations"/> class with a user-specified bucket for uploads.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        /// <param name="context">The context.</param>
        /// <param name="bucketName">Name of the bucket. This bucket should exist.</param>
        public CloudFormationBucketOperations(
            IAwsClientFactory clientFactory,
            ICloudFormationContext context,
            string bucketName)
        {
            if (bucketName != null)
            {
                this.cloudFormationBucket = new CloudFormationBucket
                                                {
                                                    BucketName = bucketName,
                                                    BucketUri = new Uri($"https://{bucketName}.s3.amazonaws.com"),
                                                    Initialized = true
                                                };
                this.context = context;
                this.clientFactory = clientFactory;
                this.timestampGenerator = context.TimestampGenerator ?? new TimestampGenerator();
            }
        }

        /// <summary>
        /// Gets the name of the bucket which is either a user supplied one, or the library's private bucket.
        /// </summary>
        /// <value>
        /// The name of the bucket.
        /// </value>
        public string BucketName => this.cloudFormationBucket.BucketName;

        /// <summary>
        /// Uploads a file to the S3 bucket associated with this instance.
        /// </summary>
        /// <param name="filePath">Path to file to upload.</param>
        /// <param name="key">The key to store in S3.</param>
        /// <returns>URI of uploaded object.</returns>
        public async Task<Uri> UploadFileToS3(string filePath, string key)
        {
            var bucket = await this.GetCloudFormationBucketAsync();
            var ub = new UriBuilder(bucket.BucketUri) { Path = $"/{key.TrimStart('/')}" };

            using (var s3 = this.clientFactory.CreateS3Client())
            {
                await s3.PutObjectAsync(
                    new PutObjectRequest
                        {
                            BucketName = this.cloudFormationBucket.BucketName, Key = key, FilePath = filePath
                        });
            }

            return ub.Uri;
        }

        /// <summary>
        /// Gets the cloud formation bucket, creating as necessary.
        /// </summary>
        /// <returns>A <see cref="cloudFormationBucket"/> object.</returns>
        internal async Task<CloudFormationBucket> GetCloudFormationBucketAsync()
        {
            if (this.cloudFormationBucket.Initialized)
            {
                return this.cloudFormationBucket;
            }

            var bucketName = this.cloudFormationBucket.BucketName;

            using (var s3 = this.clientFactory.CreateS3Client())
            {
                if ((await s3.ListBucketsAsync(new ListBucketsRequest())).Buckets.FirstOrDefault(
                        b => b.BucketName == bucketName) == null)
                {
                    // Create bucket
                    this.context.Logger.LogInformation($"Creating bucket '{bucketName}' to store uploaded templates.");
                    await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

                    try
                    {
                        // Add a lifecycle configuration to prevent buildup of old templates.
                        this.context.Logger.LogInformation(
                            "Adding lifecycle rule to purge temporary uploads after 7 days");
                        await s3.PutLifecycleConfigurationAsync(
                            new PutLifecycleConfigurationRequest
                                {
                                    BucketName = bucketName,
                                    Configuration = new LifecycleConfiguration
                                                        {
                                                            Rules = new List<LifecycleRule>
                                                                        {
                                                                            new LifecycleRule
                                                                                {
                                                                                    Id = "delete-after-one-week",
                                                                                    Filter = new LifecycleFilter
                                                                                                 {
                                                                                                     LifecycleFilterPredicate
                                                                                                         = new
                                                                                                           LifecyclePrefixPredicate
                                                                                                               {
                                                                                                                   Prefix
                                                                                                                       = string
                                                                                                                           .Empty
                                                                                                               }
                                                                                                 },
                                                                                    Status =
                                                                                        LifecycleRuleStatus.Enabled,
                                                                                    Expiration =
                                                                                        new LifecycleRuleExpiration
                                                                                            {
                                                                                                Days = 7
                                                                                            }
                                                                                }
                                                                        }
                                                        }
                                });
                    }
                    catch (Exception ex)
                    {
                        this.context.Logger.LogWarning(
                            $"Unable to add lifecycle rule to new bucket. Old temporary uploads will not be automatically purged. Exception was\n{ex.Message}");
                    }
                }
            }

            this.cloudFormationBucket.Initialized = true;
            return this.cloudFormationBucket;
        }

        /// <summary>
        /// Uploads oversize content (template or policy) to S3.
        /// </summary>
        /// <param name="stackName">Name of the stack. Use to form part of the S3 key</param>
        /// <param name="body">String content to be uploaded.</param>
        /// <param name="keySuffix">Suffix to append to S3 key</param>
        /// <param name="uploadFileType">Type of file (template or policy). Also used to form part of the S3 key.</param>
        /// <returns>URI of uploaded template.</returns>
        internal async Task<Uri> UploadCloudFormationArtifactToS3(
            string stackName,
            string body,
            string keySuffix,
            UploadFileType uploadFileType)
        {
            var bucket = await this.GetCloudFormationBucketAsync();
            var key = uploadFileType == UploadFileType.Template
                          ? this.timestampGenerator.GenerateTimestamp() + $"_{stackName}_template_" + keySuffix
                          : this.timestampGenerator.GenerateTimestamp() + $"_{stackName}_policy_" + keySuffix;

            var ub = new UriBuilder(bucket.BucketUri) { Path = $"/{key}" };

            this.context.Logger.LogInformation($"Copying oversize {uploadFileType.ToString().ToLower()} to {ub.Uri}");

            var ms = new MemoryStream(
                new UTF8Encoding().GetBytes(body ?? throw new ArgumentNullException(nameof(body))));

            using (var s3 = this.clientFactory.CreateS3Client())
            {
                await s3.PutObjectAsync(
                    new PutObjectRequest
                        {
                            BucketName = this.cloudFormationBucket.BucketName,
                            Key = key,
                            AutoCloseStream = true,
                            InputStream = ms
                        });
            }

            return ub.Uri;
        }
    }
}