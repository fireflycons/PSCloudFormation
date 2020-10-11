namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
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
        /// Regex to extract runtime version
        /// </summary>
        private static readonly Regex RuntimeVersionRegex = new Regex(@"(?<versionId>\d?\..*)");

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="dependencies">Dependencies of lambda, or <c>null</c> if none.</param>
        /// <param name="lambdaHandler">Handler as extracted from resource.</param>
        /// <param name="runtimeVersion">Version of the lambda runtime.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        protected LambdaPackager(
            FileSystemInfo lambdaArtifact,
            List<LambdaDependency> dependencies,
            string lambdaHandler,
            string runtimeVersion,
            IPSS3Util s3,
            ILogger logger)
        {
            this.Dependencies = dependencies;
            this.LambdaArtifact = lambdaArtifact;
            this.Logger = logger;
            this.S3 = s3;
            this.RuntimeVersionIdentifier = runtimeVersion;
            this.LambdaHandler = lambdaHandler;
        }

        /// <summary>
        /// Supported runtimes for packaging dependencies
        /// </summary>
        protected enum LambdaRuntime
        {
            /// <summary>
            /// Python lambda
            /// </summary>
            Python,

            /// <summary>
            /// JavaScript lambda
            /// </summary>
            Node,

            /// <summary>
            /// Ruby lambda
            /// </summary>
            Ruby,

            /// <summary>
            /// Java lambda
            /// </summary>
            Java,

            /// <summary>
            /// Go lambda
            /// </summary>
            Go,

            /// <summary>
            /// .NET lambda
            /// </summary>
            DotNet,

            /// <summary>
            /// Custom runtime lambda
            /// </summary>
            Custom
        }

        /// <summary>
        /// Gets the dependencies.
        /// </summary>
        /// <value>
        /// The dependencies.
        /// </value>
        protected List<LambdaDependency> Dependencies { get; }

        /// <summary>
        /// Gets the lambda artifact.
        /// </summary>
        /// <value>
        /// The lambda artifact.
        /// </value>
        protected FileSystemInfo LambdaArtifact { get; }

        /// <summary>
        /// Gets the logging interface.
        /// </summary>
        /// <value>
        /// The logging interface.
        /// </value>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the runtime version identifier.
        /// Used by Ruby lambdas as modules are pulled to a version specific directory.
        /// </summary>
        /// <value>
        /// The runtime version identifier.
        /// </value>
        protected string RuntimeVersionIdentifier { get; }

        /// <summary>
        /// Gets the lambda handler as defined by the resource.
        /// </summary>
        /// <value>
        /// The lambda handler.
        /// </value>
        protected string LambdaHandler { get;  }

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
        /// <param name="lambdaRuntime">Name of lambda runtime extracted from resource</param>
        /// <param name="lambdaHandler">Name of lambda handler extracted from resource</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        /// <returns>Runtime specific subclass of <see cref="LambdaPackager"/></returns>
        public static LambdaPackager CreatePackager(
            FileSystemInfo lambdaArtifact,
            string lambdaRuntime,
            string lambdaHandler,
            IPSS3Util s3,
            ILogger logger)
        {
            if (lambdaArtifact == null)
            {
                throw new ArgumentNullException(nameof(lambdaArtifact));
            }

            var lambdaDirectory = lambdaArtifact is DirectoryInfo
                                      ? lambdaArtifact.FullName
                                      : Path.GetDirectoryName(lambdaArtifact.FullName);

            var dependencyFile = Directory.GetFiles(
                lambdaDirectory
                ?? throw new InvalidOperationException("Internal error: Cannot determine location of lambda code"),
                "lambda-dependencies.*",
                SearchOption.TopDirectoryOnly).FirstOrDefault(
                f =>
                    {
                        var ext = Path.GetExtension(f);
                        return string.Compare(ext, ".json", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(
                                   ext,
                                   ".yaml",
                                   StringComparison.OrdinalIgnoreCase) == 0;
                    });

            var dependencies = dependencyFile != null ? LoadDependencies(dependencyFile) : null;

            var runtimeVersion = RuntimeVersionRegex.Match(lambdaRuntime).Groups["versionId"].Value;

            // To package dependencies, the lambda must be of a supported runtime.
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault - Intentionally so.
            switch (GetRuntime(lambdaRuntime))
            {
                case LambdaRuntime.Node:

                    return new LambdaNodePackager(lambdaArtifact, dependencies, lambdaHandler, runtimeVersion, s3, logger);

                case LambdaRuntime.Python:

                    return new LambdaPythonPackager(lambdaArtifact, dependencies, lambdaHandler, runtimeVersion, s3, logger);

                case LambdaRuntime.Ruby:

                    return new LambdaRubyPackager(lambdaArtifact, dependencies, lambdaHandler, runtimeVersion, s3, logger);

                default:

                    logger.LogWarning(
                        $"Dependency packaging for lambda runtime '{lambdaRuntime}' is currently not supported.");
                    return new LambdaPlainPackager(lambdaArtifact, null, lambdaHandler, null, s3, logger);
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
            if (this.LambdaArtifact is FileInfo fi && string.Compare(
                    Path.GetExtension(fi.Name),
                    ".zip",
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                // Already zipped
                var resourceToUpload = new ResourceUploadSettings { File = fi, Hash = fi.MD5() };

                var uploadResource = await this.S3.ObjectChangedAsync(resourceToUpload);

                return uploadResource ? resourceToUpload : null;
            }

            this.ValidateHandler();

            if (this.Dependencies == null)
            {
                switch (this.LambdaArtifact)
                {
                    case FileInfo fil:

                        return await ArtifactPackager.PackageFile(fil, workingDirectory, true, this.S3, this.Logger);

                    case DirectoryInfo di:

                        return await ArtifactPackager.PackageDirectory(di, workingDirectory, this.S3, this.Logger);
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

                throw new PackagerException($"Error deserializing {dependencyFile}: {resolvedException.Message}", resolvedException);
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
        /// If possible, validate the handler
        /// </summary>
        protected abstract void ValidateHandler();

        /// <summary>
        /// Prepares the package, accumulating any dependencies into a directory for zipping.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>Path to directory containing prepared package</returns>
        protected abstract string PreparePackage(string workingDirectory);

        /// <summary>
        /// Determine lambda runtime from file extension of Handler property of function declaration in CloudFormation.
        /// </summary>
        /// <param name="runtimeIdentifier">Name of lambda runtime extracted from resource.</param>
        /// <returns>a <see cref="LambdaRuntime"/> identifying the lambda's runtime.</returns>
        /// <exception cref="PackagerException">Unknown lambda runtime '{runtimeIdentifier}'</exception>
        private static LambdaRuntime GetRuntime(string runtimeIdentifier)
        {
            if (runtimeIdentifier.StartsWith("python"))
            {
                return LambdaRuntime.Python;
            }

            if (runtimeIdentifier.StartsWith("nodejs"))
            {
                return LambdaRuntime.Node;
            }

            if (runtimeIdentifier.StartsWith("ruby"))
            {
                return LambdaRuntime.Ruby;
            }

            if (runtimeIdentifier.StartsWith("java"))
            {
                return LambdaRuntime.Java;
            }

            if (runtimeIdentifier.StartsWith("go"))
            {
                return LambdaRuntime.Go;
            }

            if (runtimeIdentifier.StartsWith("dotnetcore"))
            {
                return LambdaRuntime.DotNet;
            }

            if (runtimeIdentifier.StartsWith("provided"))
            {
                return LambdaRuntime.Custom;
            }

            throw new PackagerException($"Unknown lambda runtime '{runtimeIdentifier}'");
        }
    }
}