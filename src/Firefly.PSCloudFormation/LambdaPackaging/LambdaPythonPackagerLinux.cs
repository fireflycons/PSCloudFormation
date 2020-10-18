namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.IO;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Non-Windows flavor Python packager.
    /// From the source to <c>venv</c> module, it appears Mac and Linux do the same, though Mac won't have lib64 symbolic link
    /// Virtual env folder structure differs from Windows in that it has bin rather than Scripts,
    /// and within the lib directory, it has an additional level for the python version.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackaging.LambdaPythonPackager" />
    internal class LambdaPythonPackagerLinux : LambdaPythonPackager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPythonPackagerLinux"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaPythonPackagerLinux(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }

        /// <summary>
        /// Gets the virtual env bin directory, which differs between Windows and non-Windows.
        /// </summary>
        /// <value>
        /// The bin directory.
        /// </value>
        protected override string BinDir { get; } = "bin";

        /// <summary>
        /// Gets the virtual env site-packages for the current OS platform.
        /// </summary>
        /// <param name="dependencyEntry">The dependency entry.</param>
        /// <returns>virtual env site-packages</returns>
        protected override string GetSitePackagesDirectory(LambdaDependency dependencyEntry)
        {
            // Linux/Mac separate site-packages according to Python version
            var sitePackages = Path.Combine(dependencyEntry.Location, this.LibDir, this.LambdaArtifact.RuntimeInfo.Runtime, "site-packages");

            if (!Directory.Exists(sitePackages))
            {
                throw new PackagerException(
                    $"'{dependencyEntry.Location} looks like a virtual env, but no 'site-packages' found for {this.LambdaArtifact.RuntimeInfo.Runtime}");
            }

            return sitePackages;
        }
    }
}