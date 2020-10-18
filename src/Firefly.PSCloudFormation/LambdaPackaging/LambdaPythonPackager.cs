﻿namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Packager for Node.JS lambda
    /// </summary>
    /// <seealso cref="LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/python-package.html"/>
    internal abstract class LambdaPythonPackager : LambdaPackager
    {
        private const string IncludeDir = "include";

        private const string Lib64Dir = "lib64";

        private const string LibDir = "lib";

        private const string ScriptsDir = "scripts";

        /// <summary>
        /// Directories that are found within a virtual env.
        /// </summary>
        private static readonly string[] VenvDirectories = { ScriptsDir, LibDir, Lib64Dir, IncludeDir };

        /// <summary>
        /// Path to temporary directory in which we build up the lambda package
        /// </summary>
        private DirectoryInfo packageDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPythonPackager"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        protected LambdaPythonPackager(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }

        /// <summary>
        /// Gets the  regex to detect lambda handler.
        /// </summary>
        /// <value>
        /// The handler regex.
        /// </value>
        protected override Regex HandlerRegex { get; } = new Regex(
            @"^\s*def\s+(?<handler>[^\d\W]\w*)\s*\(\s*[^\d\W]\w*\s*,\s*[^\d\W]\w*\s*\)\s*:",
            RegexOptions.Multiline);

        /// <summary>
        /// Gets the file extension of script files for the given lambda.
        /// </summary>
        /// <value>
        /// The script file extension.
        /// </value>
        protected override string ScriptFileExtension { get; } = ".py";

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            // Remove any temporary packaging directory.
            if (this.packageDirectory != null && Directory.Exists(this.packageDirectory.FullName))
            {
                this.packageDirectory.Delete(true);
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
        /// <exception cref="PackagerException">
        /// '{dependencyEntry.Location} looks like a virtual env, but no 'site-packages' or 'dist-packages' found.
        /// or
        /// Module {library} not found in virtual env '{dependencyEntry.Location}'
        /// or
        /// Module {libraryDirectory} not found in '{dependencyEntry.Location}'
        /// </exception>
        protected override string PreparePackage(string workingDirectory)
        {
            var dependencies = this.LambdaArtifact.LoadDependencies();

            if (!dependencies.Any())
            {
                return null;
            }

            this.packageDirectory = new DirectoryInfo(
                Path.Combine(workingDirectory, this.LambdaArtifact.LogicalName.Replace('.', '_')));

            // First, copy over the artifact
            switch (this.LambdaArtifact.ArtifactType)
            {
                case LambdaArtifactType.CodeFile:

                    FileInfo fi = this.LambdaArtifact;
                    Directory.CreateDirectory(this.packageDirectory.FullName);
                    fi.CopyTo(Path.Combine(this.packageDirectory.FullName, fi.Name));
                    break;

                case LambdaArtifactType.Directory:

                    CopyAll(this.LambdaArtifact, this.packageDirectory);
                    break;
            }

            // Now accumulate dependencies
            List<string> virtualEnvDirectories = null;

            foreach (var dependencyEntry in dependencies)
            {
                if (this.IsVirtualEnv(dependencyEntry.Location))
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

                    this.CopyDependenciesToPackageDirectory(dependencyEntry, virtualEnvDirectories);
                }
                else
                {
                    // Package is directly beneath given location
                    this.CopyDependenciesToPackageDirectory(
                        dependencyEntry,
                        new List<string> { dependencyEntry.Location });
                }

                // Remove any __pycache__
                foreach (var pycache in dependencyEntry.Libraries
                    .Where(l => Directory.Exists(Path.Combine(this.packageDirectory.FullName, l))).Select(
                        l => Directory.GetDirectories(
                            Path.Combine(this.packageDirectory.FullName, l),
                            "*__pycache__",
                            SearchOption.AllDirectories)).SelectMany(pycaches => pycaches))
                {
                    Directory.Delete(pycache, true);
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
        protected abstract bool IsVirtualEnv(string path);

        /// <summary>
        /// Copies the dependencies to package directory.
        /// </summary>
        /// <param name="dependencyEntry">The dependency entry.</param>
        /// <param name="dependencyLocations">The dependency locations - one or more directories where dependencies may be found.</param>
        /// <exception cref="PackagerException">Module {library} not found in virtual env '{dependencyEntry.Location}'</exception>
        private void CopyDependenciesToPackageDirectory(
            LambdaDependency dependencyEntry,
            IReadOnlyCollection<string> dependencyLocations)
        {
            foreach (var library in dependencyEntry.Libraries)
            {
                var libDirectory = dependencyLocations.Select(d => Path.Combine(d, library))
                    .FirstOrDefault(Directory.Exists);

                if (libDirectory != null)
                {
                    // Now copy to where we are building the package
                    var source = new DirectoryInfo(libDirectory);
                    var target = new DirectoryInfo(Path.Combine(this.packageDirectory.FullName, source.Name));
                    CopyAll(source, target);
                    continue;
                }

                // Perhaps the dependency is a single file
                var libFile = dependencyLocations.Select(d => Path.Combine(d, $"{library}.py"))
                    .FirstOrDefault(File.Exists);

                if (libFile == null)
                {
                    throw new PackagerException(
                        $"Module {library} not found in virtual env '{dependencyEntry.Location}'");
                }

                // Copy the file to the packaging directory
                File.Copy(libFile, Path.Combine(this.packageDirectory.FullName, Path.GetFileName(libFile)));
            }
        }
    }
}