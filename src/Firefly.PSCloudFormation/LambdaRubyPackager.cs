namespace Firefly.PSCloudFormation
{
    using System.IO;
    using System.Text.RegularExpressions;

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
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaRubyPackager(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }

        /// <summary>
        /// Gets the  regex to detect lambda handler.
        /// </summary>
        /// <value>
        /// The handler regex.
        /// </value>
        protected override Regex HandlerRegex { get; } = new Regex(
            @"^\s*def\s+(?<handler>[^\d\W]\w*)\s*\(\s*[^\d\W]\w*:\s*,\s*[^\d\W]\w*:\s*\)\s*",
            RegexOptions.Multiline);

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

        /// <summary>
        /// Gets the file extension of script files for the given lambda.
        /// </summary>
        /// <value>
        /// The script file extension.
        /// </value>
        protected override string ScriptFileExtension { get; } = ".rb";
    }
}