namespace Firefly.PSCloudFormation
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Packager for Node.JS lambda
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/nodejs-package.html"/>
    internal class LambdaNodePackager : LambdaSiblingModulePackager
    {
        /// <summary>
        /// Gets the  regex to detect lambda handler.
        /// </summary>
        /// <value>
        /// The handler regex.
        /// </value>
        protected override Regex HandlerRegex { get; } = new Regex(
            @"^\s*exports\.(?<handler>[\$\w]\w*)\s*=\s*(async\s+)?function\s*\(\s*[\$\w]\w*\s*(,\s*[\$\w]\w*\s*){0,2}\)",
            RegexOptions.Multiline);

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
        /// Gets the file extension of script files for the given lambda.
        /// </summary>
        /// <value>
        /// The script file extension.
        /// </value>
        protected override string ScriptFileExtension { get; } = ".js";

        /// <summary>
        /// Gets the name of the module directory (full relative path from handler script).
        /// </summary>
        /// <value>
        /// The name of the module directory.
        /// </value>
        protected override string ModuleDirectory => "node_modules";
    }
}