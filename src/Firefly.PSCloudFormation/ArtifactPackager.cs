namespace Firefly.PSCloudFormation
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.CrossPlatformZip;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Methods to package up file and directory resources for upload to S3
    /// </summary>
    internal static class ArtifactPackager
    {
        /// <summary>
        /// Package a directory to a zip.
        /// </summary>
        /// <param name="artifact">Path to artifact directory.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        /// <returns>A <see cref="ResourceUploadSettings"/></returns>
        public static async Task<ResourceUploadSettings> PackageDirectory(
            DirectoryInfo artifact,
            string workingDirectory,
            IPSS3Util s3,
            ILogger logger)
        {
            // Property value points to a directory, which must always be zipped.
            var fileToUpload = Path.Combine(workingDirectory, $"{artifact.Name}.zip");

            // Compute hash before zipping as zip hashes not idempotent due to temporally changing attributes in central directory
            var resourceToUpload = new ResourceUploadSettings
                                       {
                                           File = new FileInfo(fileToUpload), Hash = artifact.MD5(), HashMatch = true
                                       };

            if (await s3.ObjectChangedAsync(resourceToUpload))
            {
                logger.LogVerbose($"Zipping directory '{artifact.FullName}' to {Path.GetFileName(fileToUpload)}");

                Zipper.Zip(
                    new CrossPlatformZipSettings
                        {
                            Artifacts = artifact.FullName,
                            CompressionLevel = 9,
                            LogMessage = m => logger.LogVerbose(m),
                            LogError = e => logger.LogError(e),
                            ZipFile = fileToUpload,
                            TargetPlatform = ZipPlatform.Unix
                        });

                resourceToUpload.HashMatch = false;
            }

            return resourceToUpload;
        }

        /// <summary>
        /// Packages the file.
        /// </summary>
        /// <param name="artifact">Single file artifact.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="zip">if set to <c>true</c> [zip].</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        /// <returns>A <see cref="ResourceUploadSettings"/></returns>
        public static async Task<ResourceUploadSettings> PackageFile(
            FileInfo artifact,
            string workingDirectory,
            bool zip,
            IPSS3Util s3,
            ILogger logger)
        {
            ResourceUploadSettings resourceToUpload;
            if (zip && string.Compare(
                    Path.GetExtension(artifact.Name),
                    ".zip",
                    StringComparison.OrdinalIgnoreCase) != 0)
            {
                // Zip if zipping is required and file is not already zipped
                var fileToUpload = Path.Combine(
                    workingDirectory,
                    $"{Path.GetFileNameWithoutExtension(artifact.Name)}.zip");

                resourceToUpload = new ResourceUploadSettings
                                           {
                                               File = new FileInfo(fileToUpload), Hash = artifact.MD5(), HashMatch = true
                                           };


                if (!await s3.ObjectChangedAsync(resourceToUpload))
                {
                    logger.LogVerbose($"Zipping '{artifact.FullName}' to {Path.GetFileName(fileToUpload)}");

                    // Compute hash before zipping as zip hashes not idempotent due to temporally changing attributes in central directory
                    Zipper.Zip(
                        new CrossPlatformZipSettings
                            {
                                Artifacts = artifact.FullName,
                                CompressionLevel = 9,
                                LogMessage = m => logger.LogVerbose(m),
                                LogError = e => logger.LogError(e),
                                ZipFile = fileToUpload,
                                TargetPlatform = ZipPlatform.Unix
                            });

                    resourceToUpload.HashMatch = false;
                }
            }
            else
            {
                resourceToUpload = new ResourceUploadSettings { File = artifact, Hash = artifact.MD5() };
                resourceToUpload.HashMatch = !(await s3.ObjectChangedAsync(resourceToUpload));
            }

            return resourceToUpload;
        }
    }
}