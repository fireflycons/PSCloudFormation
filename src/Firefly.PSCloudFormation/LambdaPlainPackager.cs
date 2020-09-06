namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Plain packager for lambda without dependencies
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackager" />
    internal class LambdaPlainPackager : LambdaPackager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPlainPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="dependencies">Dependencies of lambda, or <c>null</c> if none.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaPlainPackager(
            FileSystemInfo lambdaArtifact,
            List<LambdaDependency> dependencies,
            IPSS3Util s3,
            ILogger logger)
            : base(lambdaArtifact, dependencies, s3, logger)
        {
        }

        /// <summary>
        /// Package a directory artifact
        /// </summary>
        /// <param name="workingDirectory">Working directory to use for packaging</param>
        /// <returns>
        ///   <see cref="ResourceUploadSettings" />; else <c>null</c> if nothing to upload (hash sums match)
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override Task<ResourceUploadSettings> PackageDirectory(string workingDirectory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prepares the package, accumulating any dependencies into a directory for zipping.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>
        /// Path to directory containing prepared package
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override string PreparePackage(string workingDirectory)
        {
            throw new NotImplementedException();
        }
    }
}