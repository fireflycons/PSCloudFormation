namespace Firefly.PSCloudFormation
{
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Object that describes a lambda dependency
    /// </summary>
    [DebuggerDisplay("{Location}")]
    internal class LambdaDependency
    {
        /// <summary>
        /// Directory containing modules to package with lambda
        /// </summary>
        private string location;

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
        /// <exception cref="DirectoryNotFoundException">Environment variable {varName} not found</exception>
        public string Location
        {
            get => this.location;
            set
            {
                if (value.StartsWith("$"))
                {
                    // Environment variable
                    var varName = value.Substring(1);
                    var envVar = Environment.GetEnvironmentVariable(varName);

                    if (string.IsNullOrEmpty(envVar))
                    {
                        throw new DirectoryNotFoundException($"Cannot find dependencies directory due to environment variable {varName} not found");
                    }

                    this.location = envVar;
                }
                else
                {
                    this.location = value;
                }
            }
        }

        /// <summary>
        /// Resolves relative path from dependency file to dependencies directory as full path.
        /// </summary>
        /// <param name="dependencyFile">The dependency file.</param>
        /// <returns>This object</returns>
        /// <exception cref="ArgumentNullException">dependencyFile is null</exception>
        /// <exception cref="DirectoryNotFoundException">Dependencies directory not found: {this.location}</exception>
        public LambdaDependency ResolveRelativePath(string dependencyFile)
        {
            if (dependencyFile == null)
            {
                throw new ArgumentNullException(nameof(dependencyFile));
            }

            var locationTemp = this.Location.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(locationTemp))
            {
                return this;
            }

            this.location = Path.GetFullPath(Path.Combine(new FileInfo(dependencyFile).DirectoryName, locationTemp));

            return this;
        }
    }
}