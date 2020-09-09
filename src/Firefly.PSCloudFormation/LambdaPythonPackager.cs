namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Packager for Node.JS lambda
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/python-package.html"/>
    internal class LambdaPythonPackager : LambdaPackager
    {
        /// <summary>
        /// Directories that are found within a virtual env.
        /// </summary>
        private static readonly string[] VenvDirectories = { "scripts", "lib", "lib64", "include" };

        /// <summary>
        /// Path to temporary directory in which we build up the lambda package
        /// </summary>
        private DirectoryInfo packageDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPythonPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="dependencies">Dependencies of lambda, or <c>null</c> if none.</param>
        /// <param name="runtimeVersion">Version of the lambda runtime.</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaPythonPackager(
            FileSystemInfo lambdaArtifact,
            List<LambdaDependency> dependencies,
            string runtimeVersion,
            IPSS3Util s3,
            ILogger logger)
            : base(lambdaArtifact, dependencies, runtimeVersion, s3, logger)
        {
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            // Remove any temporary packaging directory.
            this.packageDirectory?.Delete(true);
            base.Dispose(disposing);
        }

        /// <summary>
        /// Prepares the package, accumulating any dependencies into a directory for zipping.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>
        /// Path to directory containing prepared package
        /// </returns>
        /// <exception cref="PackagerException">
        /// '{dependencyEntry.Location} looks like a virtual env, but no 'site-packages' or 'dist-packages' found.
        /// or
        /// Module {library} not found in virtual env '{dependencyEntry.Location}'
        /// or
        /// Module {libraryDirectory} not found in '{dependencyEntry.Location}'
        /// </exception>
        protected override string PreparePackage(string workingDirectory)
        {
            if (this.Dependencies == null)
            {
                return null;
            }

            this.packageDirectory = new DirectoryInfo(Path.Combine(workingDirectory, Guid.NewGuid().ToString()));

            // First, copy over the artifact
            switch (this.LambdaArtifact)
            {
                case FileInfo fi:

                    Directory.CreateDirectory(this.packageDirectory.FullName);
                    fi.CopyTo(Path.Combine(this.packageDirectory.FullName, fi.Name));
                    break;

                case DirectoryInfo di:

                    CopyAll(di, this.packageDirectory);
                    break;
            }

            // Now accumulate dependencies
            List<string> virtualEnvDirectories = null;

            foreach (var dependencyEntry in this.Dependencies)
            {
                if (IsVirtualEnv(dependencyEntry.Location))
                {
                    if (virtualEnvDirectories == null)
                    {
                        virtualEnvDirectories = Directory
                            .GetDirectories(dependencyEntry.Location, "*site-packages", SearchOption.AllDirectories)
                            .Concat(
                                Directory.GetDirectories(
                                    dependencyEntry.Location,
                                    "*dist-packages",
                                    SearchOption.AllDirectories)).ToList();

                        if (!virtualEnvDirectories.Any())
                        {
                            throw new PackagerException(
                                $"'{dependencyEntry.Location} looks like a virtual env, but no 'site-packages' or 'dist-packages' found.");
                        }
                    }

                    foreach (var library in dependencyEntry.Libraries)
                    {
                        var libDirectory = virtualEnvDirectories.Select(d => Path.Combine(d, library))
                            .FirstOrDefault(l => Directory.Exists(l));

                        if (libDirectory == null)
                        {
                            throw new PackagerException(
                                $"Module {library} not found in virtual env '{dependencyEntry.Location}'");
                        }

                        // Now copy to where we are building the package
                        var source = new DirectoryInfo(libDirectory);
                        var target = new DirectoryInfo(Path.Combine(this.packageDirectory.FullName, source.Name));
                        CopyAll(source, target);
                    }
                }
                else
                {
                    // Package is directly beneath given location
                    foreach (var libraryDirectory in dependencyEntry.Libraries.Select(
                        l => new DirectoryInfo(Path.Combine(dependencyEntry.Location, l))))
                    {
                        if (!libraryDirectory.Exists)
                        {
                            throw new PackagerException(
                                $"Module {libraryDirectory} not found in '{dependencyEntry.Location}'");
                        }

                        var target = new DirectoryInfo(
                            Path.Combine(this.packageDirectory.FullName, libraryDirectory.Name));
                        CopyAll(libraryDirectory, this.packageDirectory);
                    }
                }

                // Remove any __pycache__
                foreach (var pycaches in dependencyEntry.Libraries.Select(
                    l => Directory.GetDirectories(
                        Path.Combine(this.packageDirectory.FullName, l),
                        "*__pycache__",
                        SearchOption.AllDirectories)))
                {
                    foreach (var pycache in pycaches)
                    {
                        Directory.Delete(pycache, true);
                    }
                }
            }

            return this.packageDirectory.FullName;
        }

        /// <summary>
        /// Determines whether the specified path is a virtual env.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if [is virtual env] [the specified path]; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// A virtual env should contain at least the following directories: Include, Lib or Lib64, Scripts
        /// </remarks>
        private static bool IsVirtualEnv(string path)
        {
            var directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .Select(d => Path.GetFileName(d).ToLowerInvariant()).ToList();

            return VenvDirectories.Intersect(directories).Count() == directories.Count;
        }
    }
}