namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

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
        /// <param name="lambdaHandler">Handler as extracted from resource.</param>
        /// <param name="runtimeVersion">Version of the lambda runtime.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaPlainPackager(
            FileSystemInfo lambdaArtifact,
            List<LambdaDependency> dependencies,
            string lambdaHandler,
            string runtimeVersion,
            IPSS3Util s3,
            ILogger logger)
            : base(lambdaArtifact, dependencies, lambdaHandler, runtimeVersion, s3, logger)
        {
        }

        /// <summary>
        /// If possible, validate the handler
        /// </summary>
        protected override void ValidateHandler()
        {
            // Do nothing
        }

        /// <summary>
        /// Prepares the package, accumulating any dependencies into a directory for zipping.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>
        /// Path to directory containing prepared package
        /// </returns>
        /// <exception cref="NotImplementedException">Not implemented for plain packages (no collectable dependencies).</exception>
        protected override string PreparePackage(string workingDirectory)
        {
            throw new NotImplementedException("Not implemented for plain packages (no collectable dependencies).");
        }
    }
}