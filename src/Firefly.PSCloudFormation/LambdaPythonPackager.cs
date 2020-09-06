namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.CrossPlatformZip;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Packager for Node.JS lambda
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/python-package.html"/>
    internal class LambdaPythonPackager : LambdaPackager
    {
        private static readonly string[] VenvDirectories = { "scripts", "lib", "lib64", "include" };

        public LambdaPythonPackager(
            FileSystemInfo lambdaArtifact,
            List<LambdaDependency> dependencies,
            IPSS3Util s3,
            ILogger logger)
            : base(lambdaArtifact, dependencies, s3, logger)
        {
        }

        protected override async Task<ResourceUploadSettings> PackageDirectory(string workingDirectory)
        {
            throw new NotImplementedException();
        }

        protected override string PreparePackage(string workingDirectory)
        {
            if (this.Dependencies == null)
            {
                return null;
            }

            var packageDirectory = new DirectoryInfo(Path.Combine(workingDirectory, Guid.NewGuid().ToString()));

            // First, copy over the artifact
            switch (this.LambdaArtifact)
            {
                case FileInfo fi:

                    Directory.CreateDirectory(packageDirectory.FullName);
                    fi.CopyTo(Path.Combine(packageDirectory.FullName, fi.Name));
                    break;

                case DirectoryInfo di:

                    LambdaPackager.CopyAll(di, packageDirectory);
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
                        var target = new DirectoryInfo(Path.Combine(packageDirectory.FullName, source.Name));
                        LambdaPackager.CopyAll(source, target);
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

                        var target = new DirectoryInfo(Path.Combine(packageDirectory.FullName, libraryDirectory.Name));
                        LambdaPackager.CopyAll(libraryDirectory, packageDirectory);
                    }
                }

                // Remove any __pycache__
                foreach (var pycaches in dependencyEntry.Libraries.Select(
                    l => Directory.GetDirectories(
                        Path.Combine(packageDirectory.FullName, l),
                        "*__pycache__",
                        SearchOption.AllDirectories)))
                {
                    foreach (var pycache in pycaches)
                    {
                        Directory.Delete(pycache, true);
                    }
                }
            }

            return packageDirectory.FullName;
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
            var directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).Select(d => Path.GetFileName(d).ToLowerInvariant()).ToList();

            return VenvDirectories.Intersect(directories).Count() == directories.Count;
        }
    }
}