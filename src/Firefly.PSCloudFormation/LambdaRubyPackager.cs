namespace Firefly.PSCloudFormation
{
    using System.Collections.Generic;
    using System.IO;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Packager for Ruby lambda
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/ruby-package.html"/>
    internal class LambdaRubyPackager : LambdaSiblingModulePackager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaRubyPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="dependencies">Dependencies of lambda, or <c>null</c> if none.</param>
        /// <param name="runtimeVersion">Version of the lambda runtime.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaRubyPackager(
            FileSystemInfo lambdaArtifact,
            List<LambdaDependency> dependencies,
            string runtimeVersion,
            IPSS3Util s3,
            ILogger logger)
            : base(lambdaArtifact, dependencies, runtimeVersion, s3, logger)
        {
        }

        /// <summary>
        /// Gets the name of the module directory.
        /// </summary>
        /// <value>
        /// The name of the module directory.
        /// </value>
        protected override string ModuleDirectory => $"vendor/bundle/ruby/{this.RuntimeVersionIdentifier}.0/cache".Replace('/', Path.DirectorySeparatorChar);
    }
}