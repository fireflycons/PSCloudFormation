namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Packager for Node.JS lambda
    /// </summary>
    /// <seealso cref="LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/nodejs-package.html"/>
    internal class LambdaNodePackager : LambdaSiblingModulePackager
    {
        /// <inheritdoc />
        protected override LambdaTraits Traits => new LambdaTraitsNode();

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaNodePackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaNodePackager(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }

        /// <summary>
        /// Gets the name of the module directory (full relative path from handler script).
        /// </summary>
        /// <value>
        /// The name of the module directory.
        /// </value>
        protected override string ModuleDirectory => "node_modules";
    }
}