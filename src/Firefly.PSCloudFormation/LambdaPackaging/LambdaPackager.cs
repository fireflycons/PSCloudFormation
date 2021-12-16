namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    using ICSharpCode.SharpZipLib.Zip;

    /// <summary>
    /// Abstract Lambda Packager
    /// </summary>
    internal abstract class LambdaPackager : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        protected LambdaPackager(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
        {
            this.LambdaArtifact = lambdaArtifact;
            this.Logger = logger;
            this.S3 = s3;
        }

        /// <summary>
        /// Gets the traits.
        /// </summary>
        /// <value>
        /// The traits.
        /// </value>
        protected abstract LambdaTraits Traits { get; }

        /// <summary>
        /// Gets the lambda artifact.
        /// </summary>
        /// <value>
        /// The lambda artifact.
        /// </value>
        protected LambdaArtifact LambdaArtifact { get; }

        /// <summary>
        /// Gets the logging interface.
        /// </summary>
        /// <value>
        /// The logging interface.
        /// </value>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the S3 interface.
        /// </summary>
        /// <value>
        /// The S3 interface.
        /// </value>
        protected IPSS3Util S3 { get; }

        /// <summary>
        /// Gets the  regex to detect lambda handler.
        /// </summary>
        /// <value>
        /// The handler regex.
        /// </value>
        protected Regex HandlerRegex => this.Traits.HandlerRegex;

        /// <summary>
        /// Gets the file extension of script files for the given lambda.
        /// </summary>
        /// <value>
        /// The script file extension.
        /// </value>
        protected string ScriptFileExtension => this.Traits.ScriptFileExtension;

        /// <summary>
        /// Factory method to create a runtime specific lambda packager.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        /// <param name="platform">Operating system platform</param>
        /// <returns>Runtime specific subclass of <see cref="LambdaPackager"/></returns>
        public static LambdaPackager CreatePackager(
            LambdaArtifact lambdaArtifact,
            IPSS3Util s3,
            ILogger logger,
            IOSInfo platform)
        {
            if (lambdaArtifact == null)
            {
                throw new ArgumentNullException(nameof(lambdaArtifact));
            }

            // To package dependencies, the lambda must be of a supported runtime.
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault - Intentionally so.
            switch (lambdaArtifact.RuntimeInfo.RuntimeType)
            {
                case LambdaRuntimeType.Node:

                    return new LambdaNodePackager(lambdaArtifact, s3, logger);

                case LambdaRuntimeType.Python:

                    // Python treats Linux and OSX the same so if not Windows, then Linux packager
                    return platform.OSPlatform == OSPlatform.Windows
                               ? (LambdaPythonPackager)new LambdaPythonPackagerWindows(lambdaArtifact, s3, logger)
                               : new LambdaPythonPackagerLinux(lambdaArtifact, s3, logger);

                case LambdaRuntimeType.Ruby:

                    return new LambdaRubyPackager(lambdaArtifact, s3, logger);

                default:

                    logger.LogWarning(
                        $"Dependency packaging for lambda runtime '{lambdaArtifact.RuntimeInfo.RuntimeType}' is currently not supported.");
                    return new LambdaPlainPackager(lambdaArtifact, s3, logger);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Package the artifact
        /// </summary>
        /// <param name="workingDirectory">Working directory to use for packaging</param>
        /// <returns><see cref="ResourceUploadSettings"/>; else <c>null</c> if nothing to upload (hash sums match)</returns>
        public async Task<ResourceUploadSettings> Package(string workingDirectory)
        {
            this.ValidateHandler();

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault - Intentionally so, all other cases are effectively the default
            switch (this.LambdaArtifact.ArtifactType)
            {
                case LambdaArtifactType.ZipFile:

                    // Already zipped
                    FileInfo lambdaFile = this.LambdaArtifact;
                    var resourceToUpload = new ResourceUploadSettings { File = lambdaFile, Hash = lambdaFile.MD5() };

                    await this.S3.ObjectChangedAsync(resourceToUpload);

                    // Template will always be modified, however the resource may not need upload.
                    return resourceToUpload;

                case LambdaArtifactType.Inline:
                case LambdaArtifactType.FromS3:
                    // Template is unchanged if code is inline or already in S3
                    return null;

                default:

                    var dependencies = this.LambdaArtifact.LoadDependencies();

                    if (!dependencies.Any())
                    {
                        switch (this.LambdaArtifact.ArtifactType)
                        {
                            case LambdaArtifactType.CodeFile:

                                return await ArtifactPackager.PackageFile(
                                           this.LambdaArtifact,
                                           workingDirectory,
                                           true,
                                           this.S3,
                                           this.Logger);

                            case LambdaArtifactType.Directory:

                                return await ArtifactPackager.PackageDirectory(
                                           this.LambdaArtifact,
                                           workingDirectory,
                                           this.S3,
                                           this.Logger);
                        }
                    }

                    // If we get here, there are dependencies to process
                    var packageDirectory = this.PreparePackage(workingDirectory);
                    return await ArtifactPackager.PackageDirectory(
                               new DirectoryInfo(packageDirectory),
                               workingDirectory,
                               this.S3,
                               this.Logger);
            }
        }

        /// <summary>
        /// Copies a directory structure from one place to another.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        protected static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (var fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each sub-directory using recursion.
            foreach (var sourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
                CopyAll(sourceSubDir, nextTargetSubDir);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Prepares the package, accumulating any dependencies into a directory for zipping.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>Path to directory containing prepared package</returns>
        protected abstract string PreparePackage(string workingDirectory);

        /// <summary>
        /// If possible, validate the handler
        /// </summary>
        private void ValidateHandler()
        {
            if (this.HandlerRegex == null)
            {
                this.Logger.LogWarning(
                    $"{this.LambdaArtifact.LogicalName}: Handler validation currently not supported for lambdas of type {this.LambdaArtifact.RuntimeInfo.RuntimeType}");
                return;
            }

            if (!this.LambdaArtifact.HandlerInfo.IsValidSignature)
            {
                throw new PackagerException(
                    $"{this.LambdaArtifact.LogicalName}: Invalid signature for handler: {this.LambdaArtifact.HandlerInfo.Handler}");
            }

            var handlerFileNameWithoutExtension = this.LambdaArtifact.HandlerInfo.FilePart;
            var methodName = this.LambdaArtifact.HandlerInfo.MethodPart;
            string moduleFileName;
            string content;

            switch (this.LambdaArtifact.ArtifactType)
            {
                case LambdaArtifactType.CodeFile:

                    FileInfo handlerScriptInfo = this.LambdaArtifact;

                    if (!handlerScriptInfo.Exists)
                    {
                        throw new FileNotFoundException(handlerScriptInfo.Name);
                    }

                    content = File.ReadAllText(handlerScriptInfo.FullName);
                    moduleFileName = handlerScriptInfo.Name;

                    break;

                case LambdaArtifactType.Directory:

                    DirectoryInfo lambdaDirectoryInfo = this.LambdaArtifact;

                    var handlerScript = Directory.GetFiles(lambdaDirectoryInfo.FullName, $"{handlerFileNameWithoutExtension}.*", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(
                            f => string.Compare(Path.GetExtension(f), this.ScriptFileExtension, StringComparison.OrdinalIgnoreCase) == 0);

                    if (handlerScript == null)
                    {
                        throw new FileNotFoundException($"{handlerFileNameWithoutExtension}{this.ScriptFileExtension}");
                    }

                    content = File.ReadAllText(handlerScript);
                    moduleFileName = Path.GetFileName(handlerScript);
                    break;

                case LambdaArtifactType.Inline:

                    if (handlerFileNameWithoutExtension != "index")
                    {
                        throw new PackagerException($"{this.LambdaArtifact.LogicalName}: Inline lambdas must have a handler beginning 'index.'");
                    }

                    content = this.LambdaArtifact.InlineCode;
                    moduleFileName = "<inline code>";
                    break;

                case LambdaArtifactType.ZipFile:

                    var zipEntryName = this.LambdaArtifact.HandlerInfo.FilePart + this.ScriptFileExtension;
                    content = null;

                    // Read the zip. Handler file must be in root directory of zip
                    // ReSharper disable once AssignNullToNotNullAttribute - Path should be set when artifact is Zip.
                    using (var archive = new ZipInputStream(File.OpenRead(this.LambdaArtifact.Path)))
                    {
                        ZipEntry entry;

                        while (content == null && (entry = archive.GetNextEntry()) != null)
                        {
                            if (entry.Name != zipEntryName)
                            {
                                continue;
                            }

                            using (var sr = new StreamReader(archive))
                            {
                                content = sr.ReadToEnd();
                            }
                        }
                    }

                    if (content == null)
                    {
                        throw new
                            PackagerException($"{this.LambdaArtifact.LogicalName}: Cannot locate {zipEntryName} in {this.LambdaArtifact.Path}");
                    }

                    moduleFileName = $"{zipEntryName} ({Path.GetFileName(this.LambdaArtifact.Path)})";
                    break;

                default:

                    this.Logger.LogWarning(
                        $"{this.LambdaArtifact.LogicalName}: Handler validation currently not supported for lambdas of type {this.LambdaArtifact.ArtifactType}");
                    return;
            }

            var mc = this.HandlerRegex.Matches(content);

            if (mc.Count == 0 || mc.Cast<Match>().All(m => m.Groups["handler"].Value != methodName))
            {
                // Debug info
                this.Logger.LogDebug($"Lambda handler validation for: {this.LambdaArtifact.LogicalName}");
                this.Logger.LogDebug($"- Handler to find: {this.LambdaArtifact.HandlerInfo.Handler}");
                this.Logger.LogDebug($"- Script: {moduleFileName}, Methods found: {mc.Count}");
                this.Logger.LogDebug("  " + string.Join("\n  ", mc.Cast<Match>().Select(m => m.Value)));

                if (this.GetType() == typeof(LambdaRubyPackager))
                {
                    this.Logger.LogWarning(
                        $"{this.LambdaArtifact.LogicalName}: Cannot locate handler method '{methodName}' in '{moduleFileName}'. If your method is within a class, validation is not yet supported for this.");
                    return;
                }

                throw new PackagerException(
                    $"{this.LambdaArtifact.LogicalName}: Cannot locate handler method '{methodName}' in '{moduleFileName}'");
            }
        }
    }
}