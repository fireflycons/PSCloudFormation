namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.IO;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Packager for Ruby lambda
    /// </summary>
    /// <seealso cref="LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/ruby-package.html"/>
    internal class LambdaRubyPackager : LambdaSiblingModulePackager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaRubyPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaRubyPackager(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }

        /// <inheritdoc />
        protected override LambdaTraits Traits => new LambdaTraitsRuby();

        /// <summary>
        /// Gets the name of the module directory.
        /// </summary>
        /// <value>
        /// The name of the module directory.
        /// </value>
        protected override string ModuleDirectory =>
            $"vendor/bundle/ruby/{this.LambdaArtifact.RuntimeInfo.RuntimeVersion}.0/cache".Replace(
                '/',
                Path.DirectorySeparatorChar);
    }
}