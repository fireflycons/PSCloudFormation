namespace Firefly.PSCloudFormation
{
    using System;
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
        private static readonly Regex HandlerRegex = new Regex(
            @"^\s*def\s+(?<handler>[^\d\W]\w*)\s*\(\s*[^\d\W]\w*:\s*,\s*[^\d\W]\w*:\s*\)\s*",
            RegexOptions.Multiline);

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
        /// If possible, validate the handler
        /// </summary>
        protected override void ValidateHandler()
        {
            if (!this.LambdaArtifact.HandlerInfo.IsValid)
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
                            f => string.Compare(Path.GetExtension(f), ".rb", StringComparison.OrdinalIgnoreCase) == 0);

                    if (file == null)
                    {
                        throw new FileNotFoundException($"{fileName}.rb");
                    }

                    content = File.ReadAllText(file);
                    moduleFileName = Path.GetFileName(file);
                    break;

                default:

                    // Will never get here unless a new subclass of FileSystemInfo appears.
                    throw new NotImplementedException(this.LambdaArtifact.GetType().FullName);
            }

            var mc = HandlerRegex.Matches(content);

            if (mc.Count == 0 || mc.Cast<Match>().All(m => m.Groups["handler"].Value != method))
            {
                this.Logger.LogWarning(
                    $"{this.LambdaArtifact.LogicalName}: Cannot locate handler method '{method}' in '{moduleFileName}'. If your method is within a class, validation is not yet supported for this.");
            }
        }
    }
}