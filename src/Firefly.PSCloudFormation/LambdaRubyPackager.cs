namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
        /// Gets the regex to detect lambda handler
        /// </summary>
        private static readonly Regex HandlerRegex = new Regex(@"^\s*def\s+(?<handler>[^\d\W]\w*)\s*\(\s*[^\d\W]\w*:\s*,\s*[^\d\W]\w*:\s*\)\s*", RegexOptions.Multiline);

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaRubyPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="dependencies">Dependencies of lambda, or <c>null</c> if none.</param>
        /// <param name="lambdaHandler">Handler as extracted from resource.</param>
        /// <param name="runtimeVersion">Version of the lambda runtime.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaRubyPackager(
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
        /// Gets the name of the module directory.
        /// </summary>
        /// <value>
        /// The name of the module directory.
        /// </value>
        protected override string ModuleDirectory => $"vendor/bundle/ruby/{this.RuntimeVersionIdentifier}.0/cache".Replace('/', Path.DirectorySeparatorChar);

        /// <summary>
        /// If possible, validate the handler
        /// </summary>
        protected override void ValidateHandler()
        {
            var handlerComponents = this.LambdaHandler.Split('.');

            if (handlerComponents.Length != 2)
            {
                throw new PackagerException($"Invalid signature for handler {this.LambdaHandler}");
            }

            var fileName = handlerComponents[0];
            var method = handlerComponents[1];
            string moduleFileName;
            string content;

            switch (this.LambdaArtifact)
            {
                case FileInfo fi:

                    if (!fi.Exists)
                    {
                        throw new FileNotFoundException(fi.Name);
                    }

                    content = File.ReadAllText(fi.FullName);
                    moduleFileName = fi.FullName;

                    break;

                case DirectoryInfo di:

                    var file = Directory.GetFiles(di.FullName, $"{fileName}.*", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(
                            f => string.Compare(Path.GetExtension(f), ".rb", StringComparison.OrdinalIgnoreCase) == 0);

                    if (file == null)
                    {
                        throw new FileNotFoundException($"{fileName}.rb");
                    }

                    content = File.ReadAllText(file);
                    moduleFileName = file;
                    break;

                default:

                    // Will never get here unless a new subclass of FileSystemInfo appears.
                    throw new NotImplementedException(this.LambdaArtifact.GetType().FullName);
            }

            var mc = HandlerRegex.Matches(content);

            if (mc.Count == 0 || mc.Cast<Match>().All(m => m.Groups["handler"].Value != method))
            {
                this.Logger.LogWarning($"Cannot locate handler method '{method}' in '{moduleFileName}'. If your method is within a class, validation is not yet supported for this.");
            }
        }
    }
}