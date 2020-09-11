namespace Firefly.PSCloudFormation
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Implements packaging where lambda's modules are in a directory that is a sibling of the handler function script (Ruby, Node)
    /// </summary>
    /// <remarks>
    /// When the package is created, then <see cref="ArtifactPackager.PackageDirectory"/> will always be used on the directory
    /// where the handler script is located as dependencies are always in a sub-directory of this location.
    /// </remarks>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackager" />
    internal abstract class LambdaSiblingModulePackager : LambdaPackager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaSiblingModulePackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="dependencies">Dependencies of lambda, or <c>null</c> if none.</param>
        /// <param name="runtimeVersion">Version of the lambda runtime.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        protected LambdaSiblingModulePackager(
            FileSystemInfo lambdaArtifact,
            List<LambdaDependency> dependencies,
            string runtimeVersion,
            IPSS3Util s3,
            ILogger logger)
            : base(lambdaArtifact, dependencies, runtimeVersion, s3, logger)
        {
        }

        /// <summary>
        /// Gets the copied modules to delete on disposal.
        /// </summary>
        /// <value>
        /// The copied modules.
        /// </value>
        protected List<DirectoryInfo> CopiedModules { get; } = new List<DirectoryInfo>();

        /// <summary>
        /// Gets the name of the module directory (full relative path from handler script).
        /// </summary>
        /// <value>
        /// The name of the module directory.
        /// </value>
        protected abstract string ModuleDirectory { get; }

        /// <summary>
        /// Gets the module base directory - the point at which to delete if removing the entire thing.
        /// </summary>
        /// <value>
        /// The module base directory.
        /// </value>
        protected string ModuleBaseDirectory => this.ModuleDirectory.Split('/', '\\').First();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            // Remove anything we brought into local modules directory
            foreach (var di in this.CopiedModules)
            {
                di.Delete(true);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Prepares the package, accumulating any dependencies into a directory for zipping.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>
        /// Path to directory containing prepared package
        /// </returns>
        /// <remarks>
        /// These projects dependencies are usually all in the right place already, i.e. the modules directory is a sibling to the handler script.
        /// Here, we temporarily bring any external packages into the modules directory while we package, then remove them again afterwards.
        /// </remarks>
        protected override string PreparePackage(string workingDirectory)
        {
            // ReSharper disable once IsExpressionAlwaysTrue - LambdaArtifact already null checked before we get here.
            var packageDirectory = this.LambdaArtifact is FileSystemInfo
                                       ? Path.GetDirectoryName(this.LambdaArtifact.FullName)
                                       : this.LambdaArtifact.FullName;
            var modulesDirectoryFullPath = Path.Combine(packageDirectory, this.ModuleDirectory);
            var haveModulesDirectory = Directory.Exists(Path.Combine(packageDirectory, this.ModuleBaseDirectory));

            if (!haveModulesDirectory)
            {
                Directory.CreateDirectory(modulesDirectoryFullPath);

                // Delete entire modules directory at end
                // For ruby we need to delete all the way down to 'vendor' which is first part of ModuleDirectory
                // Similarly for Node, down to 'node_modules'
                this.CopiedModules.Add(new DirectoryInfo(Path.Combine(packageDirectory, this.ModuleBaseDirectory)));
            }

            foreach (var dependencyEntry in this.Dependencies)
            {
                if (dependencyEntry.Location == modulesDirectoryFullPath)
                {
                    this.Logger.LogWarning(
                        $"Dependency entry specifies lambda's local {this.ModuleBaseDirectory} directory which is automatically packaged. Ignoring.");
                    continue;
                }

                foreach (var lib in dependencyEntry.Libraries)
                {
                    var source = new DirectoryInfo(Path.Combine(dependencyEntry.Location, lib));
                    var target = new DirectoryInfo(Path.Combine(modulesDirectoryFullPath, lib));

                    LambdaPackager.CopyAll(source, target);

                    if (haveModulesDirectory)
                    {
                        // Add this module to delete list only if we already have local module directory
                        this.CopiedModules.Add(target);
                    }
                }
            }

            return packageDirectory;
        }
    }
}