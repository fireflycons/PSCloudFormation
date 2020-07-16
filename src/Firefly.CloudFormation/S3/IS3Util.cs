namespace Firefly.CloudFormation.S3
{
    using System;
    using System.Threading.Tasks;

    using Firefly.CloudFormation.Model;

    /// <summary>
    /// Interface describing a method that CloudFormation operations can use to upload oversize content
    /// (template or policy document) to S3. If an implementation of this interface is not provided to the <see cref="CloudFormationBuilder"/>,
    /// then an attempt to run with oversize content (e.g. template body > 51,200 bytes), then the operation will fail.
    /// </summary>
    public interface IS3Util
    {
        /// <summary>
        /// <para>
        /// Uploads oversize content (template or policy) to S3.
        /// </para>
        /// This method will be called by create/update operations to upload oversize content to S3.
        /// <para>
        /// </para>
        /// </summary>
        /// <param name="stackName">Name of the stack. Use to form part of the S3 key</param>
        /// <param name="body">String content to be uploaded.</param>
        /// <param name="originalFilename">File name of original input file, or <c>"RawString"</c> if the input was a string rather than a file</param>
        /// <param name="uploadFileType">Type of file (template or policy). Could be used to form part of the S3 key.</param>
        /// <returns>URI of uploaded template.</returns>
        Task<Uri> UploadOversizeArtifactToS3(
            string stackName,
            string body,
            string originalFilename,
            UploadFileType uploadFileType);

        /// <summary>
        /// Gets the content of an S3 object.
        /// </summary>
        /// <param name="bucketName">Name of the bucket.</param>
        /// <param name="key">The key.</param>
        /// <returns>Object contents</returns>
        Task<string> GetS3ObjectContent(string bucketName, string key);
    }
}