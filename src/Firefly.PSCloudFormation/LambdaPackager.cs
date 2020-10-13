namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;

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
        /// Factory method to create a runtime specific lambda packager.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        /// <returns>Runtime specific subclass of <see cref="LambdaPackager"/></returns>
        public static LambdaPackager CreatePackager(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
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

                    return new LambdaPythonPackager(lambdaArtifact, s3, logger);

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
        /// Load and deserialize a dependency file
        /// </summary>
        /// <param name="dependencyFile">Path to dependency file</param>
        /// <returns>Deserialized Dependencies</returns>
        internal static List<LambdaDependency> LoadDependencies(string dependencyFile)
        {
            // Ensure input file path is absolute
            dependencyFile = Path.GetFullPath(dependencyFile);
            var content = File.ReadAllText(dependencyFile).Trim();

            // Determine if JSON
            var firstChar = content.Substring(0, 1);

            if (firstChar == "{")
            {
                // We are expecting an array, not an object
                throw new PackagerException($"{dependencyFile} contains a JSON object. Expecting array");
            }

            try
            {
                var dependencies = firstChar == "["
                                       ? JsonConvert.DeserializeObject<List<LambdaDependency>>(content)
                                       : new YamlDotNet.Serialization.Deserializer()
                                           .Deserialize<List<LambdaDependency>>(content);

                // Make dependency locations absolute
                return dependencies.Select(d => d.ResolveDependencyLocation(dependencyFile)).ToList();
            }
            catch (Exception e)
            {
                // Look for DirectoryNotFoundException raised by LambdaDependency setter to reduce stack trace
                var resolvedException = e;

                var dirException = e.FindInner<DirectoryNotFoundException>();

                if (dirException != null)
                {
                    resolvedException = dirException;
                }

                throw new PackagerException(
                    $"Error deserializing {dependencyFile}: {resolvedException.Message}",
                    resolvedException);
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

            // Copy each subdirectory using recursion.
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
        protected abstract void ValidateHandler();
    }
}