﻿namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.SecurityToken.Model;

    using Firefly.CloudFormation.S3;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Class to manage interaction with S3, both for the CloudFormation packager in this solution
    /// and to pass to <c>Firefly.PSCloudFormation</c> as its interface for managing oversize template/policy documents.
    /// </summary>
    /// <seealso cref="Firefly.CloudFormation.S3.IS3Util" />
    internal class S3Util : IS3Util
    {
        /// <summary>
        /// Regex to extract name and version from S3 Key
        /// </summary>
        private static readonly Regex ObjectVersionRegex = new Regex(@"(?<name>.*)-(?<version>\d+)$");

        /// <summary>
        /// The client factory
        /// </summary>
        private readonly IPSAwsClientFactory clientFactory;

        /// <summary>
        /// The cloud formation bucket
        /// </summary>
        private readonly CloudFormationBucket cloudFormationBucket;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The timestamp generator. Unit tests supply an alternate so object names may be validated.
        /// </summary>
        private readonly ITimestampGenerator timestampGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="S3Util"/> class using the
        /// library's private bucket which will be created if it doesn't exist and a 7 day lifecycle rule
        /// applied to prevent build-up of temporary objects.
        /// </summary>
        /// <param name="clientFactory">The AWS client factory.</param>
        /// <param name="context">The context.</param>
        public S3Util(IPSAwsClientFactory clientFactory, IPSCloudFormationContext context)
        {
            this.logger = context.Logger;
            this.clientFactory = clientFactory;
            this.timestampGenerator = context.TimestampGenerator ?? new TimestampGenerator();

            using (var sts = this.clientFactory.CreateSTSClient())
            {
                var account = sts.GetCallerIdentityAsync(new GetCallerIdentityRequest()).Result.Account;
                var bucketName = $"cf-templates-pscloudformation-{context.Region.SystemName}-{account}";

                this.cloudFormationBucket = new CloudFormationBucket
                                                {
                                                    BucketName = bucketName,
                                                    BucketUri = new Uri($"https://{bucketName}.s3.amazonaws.com"),
                                                    Initialized = false
                                                };
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="S3Util"/> class.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="rootTemplate">The root template.</param>
        /// <param name="bucketName">Name of the bucket.</param>
        /// <exception cref="ArgumentNullException">rootTemplate is null</exception>
        public S3Util(IPSAwsClientFactory clientFactory, ILogger logger, string rootTemplate, string bucketName)
        {
            this.BucketName = bucketName;
            this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Generate a hash of the root template filename to use as part of uploaded file keys
            // to identify this package 'project'
            this.ProjectId = GenerateProjectId(rootTemplate ?? throw new ArgumentNullException(nameof(rootTemplate)));
            this.logger?.LogDebug($"Project ID for this template is {this.ProjectId}");
        }

        /// <summary>
        /// Gets the bucket name
        /// </summary>
        public string BucketName { get; }

        /// <summary>
        /// Gets the project identifier.
        /// </summary>
        /// <value>
        /// The project identifier.
        /// </value>
        public string ProjectId { get; }

        /// <summary>
        /// Gets the content of an S3 object.
        /// </summary>
        /// <param name="bucketName">Name of the bucket.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        /// Object contents
        /// </returns>
        public async Task<string> GetS3ObjectContent(string bucketName, string key)
        {
            using (var s3 = this.clientFactory.CreateS3Client())
            {
                using (var response =
                    await s3.GetObjectAsync(new GetObjectRequest { BucketName = bucketName, Key = key }))
                {
                    using (var sr = new StreamReader(response.ResponseStream))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// <para>
        /// Uploads oversize content (template or policy) to S3.
        /// </para>
        /// This method will be called by create/update operations to upload oversize content to S3.
        /// <para></para>
        /// </summary>
        /// <param name="stackName">Name of the stack. Use to form part of the S3 key</param>
        /// <param name="body">String content to be uploaded.</param>
        /// <param name="originalFilename">File name of original input file, or <c>"RawString"</c> if the input was a string rather than a file</param>
        /// <param name="uploadFileType">Type of file (template or policy). Could be used to form part of the S3 key.</param>
        /// <returns>
        /// URI of uploaded template.
        /// </returns>
        public async Task<Uri> UploadOversizeArtifactToS3(
            string stackName,
            string body,
            string originalFilename,
            UploadFileType uploadFileType)
        {
            var bucket = await this.GetCloudFormationBucketAsync();
            var key = uploadFileType == UploadFileType.Template
                          ? this.timestampGenerator.GenerateTimestamp() + $"_{stackName}_template_" + originalFilename
                          : this.timestampGenerator.GenerateTimestamp() + $"_{stackName}_policy_" + originalFilename;

            var ub = new UriBuilder(bucket.BucketUri) { Path = $"/{key}" };

            this.logger.LogInformation($"Copying oversize {uploadFileType.ToString().ToLower()} to {ub.Uri}");

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

        /// <summary>
        /// Uploads the resource to s3 asynchronous.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="keyPrefix">The key prefix.</param>
        /// <param name="metadata">Additional metadata to add to S3 objects.</param>
        /// <returns>URL of object in S3</returns>
        /// <exception cref="FormatException">Unable to parse key name '{Path.GetFileName(latestVersion.Key)}'</exception>
        public async Task<S3Artifact> UploadResourceToS3Async(FileInfo filePath, string keyPrefix, IDictionary metadata)
        {
            var prefix = keyPrefix ?? string.Empty;

            using (var s3 = this.clientFactory.CreateS3Client())
            {
                var latestVersion = await this.GetLatestVersionOfObjectAsync(filePath, prefix);

                this.logger.LogDebug(
                    latestVersion != null
                        ? $"Version of {filePath} exists: {ToS3Url(latestVersion.BucketName, latestVersion.Key)}, ETag: {latestVersion.ETag}"
                        : $"Version of {filePath} not found");

                if (!ObjectChanged(filePath, latestVersion))
                {
                    // Artifact is unchanged. Return the most recent version.
                    this.logger.LogDebug($"{filePath.Name} is unchanged in S3");

                    return new S3Artifact
                               {
                                   // ReSharper disable once PossibleNullReferenceException If latestVersion is null, ObjectChanged will be true
                                   BucketName = latestVersion.BucketName,
                                   Key = latestVersion.Key,
                                   Url = ToS3Url(latestVersion.BucketName, latestVersion.Key)
                               };
                }

                this.logger.LogDebug($"{filePath.Name} is newer than S3 copy. Will upload.");
                string newObjectName;

                if (latestVersion == null)
                {
                    newObjectName = this.FileInfoToUnVersionedObjectName(filePath) + "-0000"
                                                                                   + Path.GetExtension(filePath.Name);
                }
                else
                {
                    // Generate new key name so that CloudFormation will redeploy lambdas etc.
                    var mc = ObjectVersionRegex.Match(Path.GetFileNameWithoutExtension(latestVersion.Key));

                    if (mc.Success)
                    {
                        // We aren't going to run this package more than 10,000 times?
                        newObjectName =
                            $"{mc.Groups["name"].Value}-{(int.Parse(mc.Groups["version"].Value) + 1) % 10000:D4}{Path.GetExtension(latestVersion.Key)}";
                    }
                    else
                    {
                        throw new FormatException($"Unable to parse key name '{Path.GetFileName(latestVersion.Key)}'");
                    }
                }

                // Now upload it
                var key = (prefix.Trim('/') + "/" + newObjectName).TrimStart('/');
                var url = ToS3Url(this.BucketName, key);
                var req = new PutObjectRequest
                              {
                                  BucketName = this.BucketName, Key = key, FilePath = filePath.FullName
                              };

                if (metadata != null)
                {
                    // Add user metadata
                    foreach (var k in metadata.Keys)
                    {
                        if (!(k is string))
                        {
                            throw new InvalidDataException("Metadata keys must be strings.");
                        }

                        req.Metadata.Add(k.ToString(), metadata[k].ToString());
                    }
                }

                await s3.PutObjectAsync(req);

                this.logger.LogVerbose($"Uploaded {filePath.Name} to {url}");
                return new S3Artifact { BucketName = this.BucketName, Key = key, Url = ToS3Url(this.BucketName, key) };
            }
        }

        /// <summary>
        /// Generates the project identifier to (hopefully) uniquely identify S3 artifacts associated with a package's root template
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The project ID></returns>
        internal static string GenerateProjectId(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            // Generate a hash of the root template filename to use as part of uploaded file keys
            // to identify this package 'project'
            using (var md5 = MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(filePath))))
                    .Replace("-", string.Empty).ToLowerInvariant();
            }
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
                    this.logger.LogInformation($"Creating bucket '{bucketName}' to store uploaded templates.");
                    await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

                    try
                    {
                        // Add a lifecycle configuration to prevent buildup of old templates.
                        this.logger.LogInformation("Adding lifecycle rule to purge temporary uploads after 7 days");
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
                        this.logger.LogWarning(
                            $"Unable to add lifecycle rule to new bucket. Old temporary uploads will not be automatically purged. Exception was\n{ex.Message}");
                    }
                }
            }

            this.cloudFormationBucket.Initialized = true;
            return this.cloudFormationBucket;
        }

        /// <summary>
        /// Determine if the object to upload differs from the object in S3
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="latestVersion"><see cref="S3Object"/> representing latest version.</param>
        /// <returns><c>true</c> if the file is different; else <c>false</c></returns>
        /// <remarks>
        /// When comparing zip files, this may always return a positive result as fields within zip directory
        /// such as time stamps may differ even if the contents are the same, i.e. zipping
        /// the same thing twice can be a non-idempotent operation.
        /// </remarks>
        // ReSharper disable once SuggestBaseTypeForParameter - It is explicitly this type
        private static bool ObjectChanged(FileInfo filePath, S3Object latestVersion)
        {
            if (latestVersion == null)
            {
                return true;
            }

            return latestVersion.ETag.Unquote().ToLowerInvariant() != filePath.MD5();
        }

        /// <summary>
        /// Converts bucket and key to S3 URL.
        /// </summary>
        /// <param name="bucket">The bucket.</param>
        /// <param name="key">The key.</param>
        /// <returns>S3 URL.</returns>
        private static string ToS3Url(string bucket, string key)
        {
            return $"https://{bucket}.s3.amazonaws.com/{key}";
        }

        /// <summary>
        /// Gets a partial S3 object name without extension or version number using this instance's project ID
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>Object name</returns>
        // ReSharper disable once SuggestBaseTypeForParameter - It is explicitly this type
        private string FileInfoToUnVersionedObjectName(FileInfo filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath.Name) + "-" + this.ProjectId;
        }

        /// <summary>
        /// Gets the latest version of an S3 object based on the artifact file name and package's root template..
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="keyPrefix">The key prefix.</param>
        /// <returns>a <see cref="S3Object"/> that is the latest version if exists; else <c>null</c>.</returns>
        private async Task<S3Object> GetLatestVersionOfObjectAsync(FileInfo filePath, string keyPrefix)
        {
            using (var s3 = this.clientFactory.CreateS3Client())
            {
                var ext = Path.GetExtension(filePath.Name);

                return (await s3.ListObjectsV2Async(
                            new ListObjectsV2Request
                                {
                                    BucketName = this.BucketName,
                                    Prefix = keyPrefix.TrimEnd('/') + '/'
                                                                    + this.FileInfoToUnVersionedObjectName(filePath)
                                })).S3Objects.Where(o => o.Key.EndsWith(ext)).OrderByDescending(o => o.Key)
                    .FirstOrDefault();
            }
        }
    }
}