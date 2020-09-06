namespace Firefly.PSCloudFormation
{
    using System;
    using System.IO;

    /// <summary>
    /// Object that describes a lambda dependency
    /// </summary>
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
                        throw new DirectoryNotFoundException($"Environment variable {varName} not found");
                    }

                    this.location = envVar;
                }
                else
                {
                    this.location = value;
                }
            }
        }

        public LambdaDependency ResolveRelativePath(string dependencyFile)
        {
            if (dependencyFile == null)
            {
                throw new ArgumentNullException(nameof(dependencyFile));
            }

            if (Path.IsPathRooted(this.location))
            {
                return this;
            }

            this.location = Path.GetFullPath(Path.Combine(new FileInfo(dependencyFile).DirectoryName, this.location));
            return this;
        }
    }
}