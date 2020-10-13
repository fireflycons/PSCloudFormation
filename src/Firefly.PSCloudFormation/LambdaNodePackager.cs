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
        /// Gets the regex to detect lambda handler
        /// </summary>
        private static readonly Regex HandlerRegex = new Regex(
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
            if (!this.LambdaArtifact.HandlerInfo.IsValidSignature)
            {
                throw new PackagerException(
                    $"{this.LambdaArtifact.LogicalName}: Invalid signature for handler: {this.LambdaArtifact.HandlerInfo.Handler}");
            }

            var fileName = this.LambdaArtifact.HandlerInfo.FilePart;
            var method = this.LambdaArtifact.HandlerInfo.MethodPart;
            string moduleFileName;
            string content;

            switch (this.LambdaArtifact.ArtifactType)
            {
                case LambdaArtifactType.CodeFile:

                    FileInfo fi = this.LambdaArtifact;

                    if (!fi.Exists)
                    {
                        throw new FileNotFoundException(fi.Name);
                    }

                    content = File.ReadAllText(fi.FullName);
                    moduleFileName = fi.FullName;

                    break;

                case LambdaArtifactType.Directory:

                    DirectoryInfo di = this.LambdaArtifact;

                    var file = Directory.GetFiles(di.FullName, $"{fileName}.*", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(
                            f => string.Compare(Path.GetExtension(f), ".js", StringComparison.OrdinalIgnoreCase) == 0);

                    if (file == null)
                    {
                        throw new FileNotFoundException($"{fileName}.js");
                    }

                    content = File.ReadAllText(file);
                    moduleFileName = Path.GetFileName(file);
                    break;

                case LambdaArtifactType.Inline:

                    if (fileName != "index")
                    {
                        throw new PackagerException($"{this.LambdaArtifact.LogicalName}: Inline lambdas must have a handler beginning 'index.'");
                    }

                    content = this.LambdaArtifact.InlineCode;
                    moduleFileName = "<inline code>";
                    break;

                default:

                    this.Logger.LogWarning(
                        $"{this.LambdaArtifact.LogicalName}: Handler validation currently not supported for lambdas of type {this.LambdaArtifact.ArtifactType}");
                    return;
            }

            var mc = HandlerRegex.Matches(content);

            if (mc.Count == 0 || mc.Cast<Match>().All(m => m.Groups["handler"].Value != method))
            {
                throw new PackagerException(
                    $"{this.LambdaArtifact.LogicalName}: Cannot locate handler method 'exports.{method}' in '{moduleFileName}'");
            }
        }
    }
}