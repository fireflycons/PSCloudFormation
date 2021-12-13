namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Base packager for Python
    /// </summary>
    /// <seealso cref="LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/python-package.html"/>
    internal abstract class LambdaPythonPackager : LambdaPackager
    {
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

        /// <inheritdoc />
        protected override LambdaTraits Traits => new LambdaTraitsPython();

        /// <summary>
        /// Gets the virtual env include directory.
        /// </summary>
        /// <value>
        /// The include directory.
        /// </value>
        protected string IncludeDir { get; } = "Include";

        /// <summary>
        /// Gets the virtual env library directory.
        /// </summary>
        /// <value>
        /// The library directory.
        /// </value>
        protected string LibDir { get; } = "lib";

        /// <summary>
        /// Gets the virtual env bin directory, which differs between Windows and non-Windows.
        /// </summary>
        /// <value>
        /// The bin directory.
        /// </value>
        protected abstract string BinDir { get; }

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
            string sitePackagesDirectory = null;

            foreach (var dependencyEntry in dependencies)
            {
                if (this.IsVirtualEnv(dependencyEntry.Location))
                {
                    if (sitePackagesDirectory == null)
                    {
                        sitePackagesDirectory = this.GetSitePackagesDirectory(dependencyEntry);
                    }

                    this.CopyDependenciesToPackageDirectory(dependencyEntry, sitePackagesDirectory);
                }
                else
                {
                    // Package is directly beneath given location
                    this.CopyDependenciesToPackageDirectory(
                        dependencyEntry,
                        dependencyEntry.Location);
                }

                // Remove any __pycache__
                foreach (var pycache in Directory.EnumerateDirectories(this.packageDirectory.FullName, "__pycache__", SearchOption.AllDirectories))
                {
                    Directory.Delete(pycache, true);
                }
            }

            return this.packageDirectory.FullName;
        }

        /// <summary>
        /// Gets the virtual env site-packages for the current OS platform.
        /// </summary>
        /// <param name="dependencyEntry">The dependency entry.</param>
        /// <returns>virtual env site-packages</returns>
        protected abstract string GetSitePackagesDirectory(LambdaDependency dependencyEntry);

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
        protected bool IsVirtualEnv(string path)
        {
            var venvDirectories = new[] { this.IncludeDir, this.LibDir, this.BinDir }.Select(d => d.ToLowerInvariant())
                .OrderBy(d => d).ToList();

            var sitePackages = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .Select(d => Path.GetFileName(d).ToLowerInvariant()).OrderBy(d => d);

            return venvDirectories
                .Intersect(
                    sitePackages)
                .SequenceEqual(venvDirectories);
        }

        /// <summary>
        /// Copies the dependencies to package directory.
        /// </summary>
        /// <param name="dependencyEntry">The dependency entry.</param>
        /// <param name="dependencyLocation">The dependency locations - one or more directories where dependencies may be found.</param>
        /// <exception cref="PackagerException">Module {library} not found in virtual env '{dependencyEntry.Location}'</exception>
        private void CopyDependenciesToPackageDirectory(
            LambdaDependency dependencyEntry,
            string dependencyLocation)
        {
            // Make this an array to use Linq to do path combine and existence check in one
            var dependencyLocationArray = new[] { dependencyLocation };

            foreach (var library in dependencyEntry.Libraries)
            {
                if (Path.IsPathRooted(library))
                {
                    if (File.Exists(library))
                    {
                        File.Copy(library, Path.Combine(this.packageDirectory.FullName, Path.GetFileName(library)));
                    }
                    else if (Directory.Exists(library))
                    {
                        var source = new DirectoryInfo(library);
                        var target = new DirectoryInfo(Path.Combine(this.packageDirectory.FullName, source.Name));
                        CopyAll(source, target);
                    }
                    else
                    {
                        throw new FileNotFoundException("Library module not found", library);
                    }
                }
                else
                {
                    var libDirectory = dependencyLocationArray.Select(d => Path.Combine(d, library))
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
                    var libFile = dependencyLocationArray.Select(d => Path.Combine(d, $"{library}.py"))
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
}