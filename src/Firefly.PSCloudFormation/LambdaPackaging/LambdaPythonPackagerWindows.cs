namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.IO;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    internal class LambdaPythonPackagerWindows : LambdaPythonPackager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPythonPackagerWindows"/> class.
        /// </summary>
        /// <param name="lambdaArtifact">The lambda artifact to package</param>
        /// <param name="s3">Interface to S3</param>
        /// <param name="logger">Interface to logger.</param>
        public LambdaPythonPackagerWindows(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }

        /// <summary>
        /// Gets the virtual env bin directory, which differs between Windows and non-Windows.
        /// </summary>
        /// <value>
        /// The bin directory.
        /// </value>
        protected override string BinDir { get; } = "scripts";

        /// <summary>
        /// Gets the virtual env site-packages for the current OS platform.
        /// </summary>
        /// <param name="dependencyEntry">The dependency entry.</param>
        /// <returns>virtual env site-packages</returns>
        protected override string GetSitePackagesDirectory(LambdaDependency dependencyEntry)
        {
            var sitePackages = Path.Combine(dependencyEntry.Location, this.LibDir, "site-packages");

            if (!Directory.Exists(sitePackages))
            {
                throw new PackagerException(
                    $"'{dependencyEntry.Location} looks like a virtual env, but no 'site-packages' found.");
            }

            return sitePackages;
        }
    }
}