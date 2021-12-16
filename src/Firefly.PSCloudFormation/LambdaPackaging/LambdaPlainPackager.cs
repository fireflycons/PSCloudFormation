namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Plain packager for lambda without dependencies
    /// </summary>
    /// <seealso cref="LambdaPackager" />
    internal class LambdaPlainPackager : LambdaPackager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPlainPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaPlainPackager(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }


        /// <inheritdoc />
        protected override LambdaTraits Traits => new LambdaTraitsPlain();

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