namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Object that describes a lambda dependency
    /// </summary>
    [DebuggerDisplay("{Location}")]
    internal class LambdaDependency : IEquatable<LambdaDependency>
    {
        /// <summary>
        /// Gets or sets the libraries to package with the lambda.
        /// </summary>
        /// <value>
        /// The libraries.
        /// </value>
        public string[] Libraries { get; set; }

        /// <summary>
        /// Gets or sets the location of the lambda libraries.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public string Location { get; set; }

        /// <summary>
        /// Resolves relative path and environment variables from dependency file to dependencies directory as full path.
        /// </summary>
        /// <param name="dependencyFile">The dependency file.</param>
        /// <returns>This object</returns>
        /// <exception cref="ArgumentNullException">dependencyFile is null</exception>
        /// <exception cref="DirectoryNotFoundException">Environment variable {name} not found</exception>
        public LambdaDependency ResolveDependencyLocation(string dependencyFile)
        {
            if (Path.IsPathRooted(this.Location))
            {
                // Already resolved
                return this;
            }

            if (dependencyFile == null)
            {
                throw new ArgumentNullException(nameof(dependencyFile));
            }

            var locationTemp = this.Location;

            if (locationTemp.StartsWith("$"))
            {
                var varName = locationTemp.Substring(1);
                var envVar = Environment.GetEnvironmentVariable(varName);

                if (string.IsNullOrEmpty(envVar))
                {
                    throw new DirectoryNotFoundException(
                        $"Cannot find dependencies directory due to environment variable {varName} not found");
                }

                locationTemp = envVar;
            }

            locationTemp = locationTemp.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            this.Location = Path.IsPathRooted(locationTemp)
                                ? locationTemp
                                : Path.GetFullPath(
                                    Path.Combine(new FileInfo(dependencyFile).DirectoryName, locationTemp));

            return this;
        }

        public bool Equals(LambdaDependency other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.LibrariesEquals(other) && this.Location == other.Location;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((LambdaDependency)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Libraries != null ? string.Join(":", this.Libraries.OrderBy(l => l)).GetHashCode() : 0) * 397) ^ (this.Location != null ? this.Location.GetHashCode() : 0);
            }
        }

        private bool LibrariesEquals(LambdaDependency other)
        {
            if (this.Libraries == null & other.Libraries == null)
            {
                return true;
            }

            if (this.Libraries == null && other.Libraries != null)
            {
                return false;
            }

            if (this.Libraries != null && other.Libraries == null)
            {
                return false;
            }

            return this.Libraries.OrderBy(l => l).SequenceEqual(other.Libraries.OrderBy(l => l));
        }
    }
}