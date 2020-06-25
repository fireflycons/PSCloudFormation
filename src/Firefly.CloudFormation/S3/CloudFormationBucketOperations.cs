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
    using Amazon.SecurityToken;
    using Amazon.SecurityToken.Model;

    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Manages operations on the bucket in which oversize templates are uploaded to
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class CloudFormationBucketOperations
    {
        /// <summary>
        /// The client
        /// </summary>
        private readonly IAmazonS3 client;

        /// <summary>
        /// The cloud formation bucket name
        /// </summary>
        private readonly string cloudFormationBucketName;

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
        private CloudFormationBucket cloudFormationBucket;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationBucketOperations"/> class.
        /// </summary>
        /// <param name="s3Client">The s3 client.</param>
        /// <param name="stsClient">The STS client.</param>
        /// <param name="context">The context.</param>
        public CloudFormationBucketOperations(IAmazonS3 s3Client, IAmazonSecurityTokenService stsClient, ICloudFormationContext context)
        {
            this.context = context;

            var task = stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
            task.Wait();

            this.cloudFormationBucketName =
                // ReSharper disable once StringLiteralTypo
                $"cf-templates-pscloudformation-{context.Region.SystemName}-{task.Result.Account}";

            this.timestampGenerator = context.TimestampGenerator ?? new TimestampGenerator();

            this.client = s3Client;
        }

        /// <summary>
        /// Gets the cloud formation bucket, creating as necessary.
        /// </summary>
        /// <returns>A <see cref="cloudFormationBucket"/> object.</returns>
        public async Task<CloudFormationBucket> GetCloudFormationBucketAsync()
        {
            if (this.cloudFormationBucket == null)
            {
                if ((await PollyHelper.ExecuteWithPolly(() => this.client.ListBucketsAsync(new ListBucketsRequest())))
                    .Buckets.FirstOrDefault(b => b.BucketName == this.cloudFormationBucketName) == null)
                {
                    // Create bucket
                    await PollyHelper.ExecuteWithPolly(
                        () => this.client.PutBucketAsync(
                            new PutBucketRequest { BucketName = this.cloudFormationBucketName }));

                    // Add a lifecycle configuration to prevent buildup of old templates.
                    await PollyHelper.ExecuteWithPolly(
                        () => this.client.PutLifecycleConfigurationAsync(
                            new PutLifecycleConfigurationRequest
                                {
                                    BucketName = this.cloudFormationBucketName,
                                    Configuration = new LifecycleConfiguration
                                                        {
                                                            Rules = new List<LifecycleRule>
                                                                        {
                                                                            new LifecycleRule
                                                                                {
                                                                                    Expiration =
                                                                                        new LifecycleRuleExpiration
                                                                                            {
                                                                                                Days = 7
                                                                                            }
                                                                                }
                                                                        }
                                                        }
                                }));

                    this.context.Logger.LogInformation(
                        $"Created S3 bucket {this.cloudFormationBucketName} to store oversize templates.");
                }

                this.cloudFormationBucket = new CloudFormationBucket
                                                {
                                                    BucketName = this.cloudFormationBucketName,
                                                    BucketUri = new Uri(
                                                        $"https://{this.cloudFormationBucketName}.s3.amazonaws.com")
                                                };
            }

            return this.cloudFormationBucket;
        }

        /// <summary>
        /// Uploads the oversize file to the bucket managed by this class.
        /// </summary>
        /// <param name="stackName">Name of the stack.</param>
        /// <param name="filePath">The file location.</param>
        /// <param name="uploadFileType">Type of file (template or policy).</param>
        /// <returns>URI of uploaded template, or <c>null</c> if the file did not require upload.</returns>
        public async Task<Uri> UploadFileToS3(string stackName, string filePath, UploadFileType uploadFileType)
        {
            var bucket = await this.GetCloudFormationBucketAsync();
            var key = uploadFileType == UploadFileType.Template
                          ? this.timestampGenerator.GenerateTimestamp() + $"_{stackName}_template_"
                                                                        + Path.GetFileName(filePath)
                          : this.timestampGenerator.GenerateTimestamp() + $"_{stackName}_policy_"
                                                                        + Path.GetFileName(filePath);

            var ub = new UriBuilder(bucket.BucketUri) { Path = $"/{key}" };

            this.context.Logger.LogInformation($"Copying oversize {uploadFileType.ToString().ToLower()} to {bucket.BucketUri}");

            await PollyHelper.ExecuteWithPolly(
                () => this.client.PutObjectAsync(
                    new PutObjectRequest
                        {
                            BucketName = this.cloudFormationBucketName,
                            Key = key,
                            FilePath = filePath
                        }));

            return ub.Uri;
        }

        /// <summary>
        /// Uploads the oversize file to the bucket managed by this class.
        /// </summary>
        /// <param name="stackName">Name of the stack.</param>
        /// <param name="body">String content to be uploaded.</param>
        /// <param name="uploadFileType">Type of file (template or policy).</param>
        /// <returns>URI of uploaded template, or <c>null</c> if the file did not require upload.</returns>
        public async Task<Uri> UploadStringToS3(string stackName, string body, UploadFileType uploadFileType)
        {
            var bucket = await this.GetCloudFormationBucketAsync();
            var key = uploadFileType == UploadFileType.Template
                          ? this.timestampGenerator.GenerateTimestamp() + $"_{stackName}_template_"
                                                                        + Path.GetFileName(body)
                          : this.timestampGenerator.GenerateTimestamp() + $"_{stackName}_policy_"
                                                                        + Path.GetFileName(body);

            var ub = new UriBuilder(bucket.BucketUri) { Path = $"/{key}" };

            this.context.Logger.LogInformation($"Copying oversize {uploadFileType.ToString().ToLower()} to {bucket.BucketUri}");

            var ms = new MemoryStream(new UTF8Encoding().GetBytes(body ?? throw new ArgumentNullException(nameof(body))));

            await this.client.PutObjectAsync(
                new PutObjectRequest
                    {
                        BucketName = this.cloudFormationBucketName, Key = key, AutoCloseStream = true, InputStream = ms
                    });

            return ub.Uri;
        }
    }
}