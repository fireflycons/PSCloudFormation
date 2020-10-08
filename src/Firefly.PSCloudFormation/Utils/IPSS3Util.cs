namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;

    /// <summary>
    /// Interface for this library's implementation of <c>IS3Util</c>
    /// </summary>
    /// <seealso cref="Firefly.CloudFormation.IS3Util" />
    // ReSharper disable once InconsistentNaming
    internal interface IPSS3Util : IS3Util
    {
        /// <summary>
        /// Determine whether we have created a new version of the resource in S3
        /// </summary>
        /// <param name="resourceToUpload">The resource to upload.</param>
        /// <returns><c>true</c> if the object should be uploaded; else <c>false</c></returns>
        /// <exception cref="FormatException">Unable to parse key name '{Path.GetFileName(latestVersion.Key)}'</exception>
        Task<bool> ObjectChangedAsync(ResourceUploadSettings resourceToUpload);

        /// <summary>
        /// Uploads the resource to s3 asynchronous.
        /// </summary>
        /// <param name="resourceToUpload"><see cref="ResourceUploadSettings"/> describing resource to upload to S3</param>
        /// <returns>URL of object in S3</returns>
        /// <exception cref="FormatException">Unable to parse key name '{Path.GetFileName(latestVersion.Key)}'</exception>
        Task<S3Artifact> UploadResourceToS3Async(ResourceUploadSettings resourceToUpload);

        string KeyPrefix { get; }

        IDictionary Metadata { get; }
    }
}