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
    /// Packager for Node.JS lambda
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/nodejs-package.html"/>
    internal class LambdaNodePackager : LambdaSiblingModulePackager
    {
        /// <summary>
        /// Gets the regex to detect lambda handler
        /// </summary>
        private static readonly Regex HandlerRegex = new Regex(
            @"^\s*exports\.(?<handler>[\$\w]\w*)\s*=\s*(async\s+)?function\s*\(\s*[\$\w]\w*\s*(,\s*[\$\w]\w*\s*){0,2}\)",
            RegexOptions.Multiline);

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaNodePackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="dependencies">Dependencies of lambda, or <c>null</c> if none.</param>
        /// <param name="lambdaHandler">Handler as extracted from resource.</param>
        /// <param name="runtimeVersion">Version of the lambda runtime.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaNodePackager(
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
        /// Gets the name of the module directory (full relative path from handler script).
        /// </summary>
        /// <value>
        /// The name of the module directory.
        /// </value>
        protected override string ModuleDirectory => "node_modules";

        /// <summary>
        /// If possible, validate the handler
        /// </summary>
        /// <exception cref="FileNotFoundException">Module containing handler not found</exception>
        /// <exception cref="System.NotImplementedException">Unknown subclass of <see cref="FileSystemInfo"/></exception>
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
                            f => string.Compare(Path.GetExtension(f), ".js", StringComparison.OrdinalIgnoreCase) == 0);

                    if (file == null)
                    {
                        throw new FileNotFoundException($"{fileName}.js");
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
                throw new PackagerException($"Cannot locate handler method '{method}' in '{moduleFileName}'");
            }
        }
    }
}