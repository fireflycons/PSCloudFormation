namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Firefly.CloudFormation.Parsers;
    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;

    /// <summary>
    /// Given a resource, decide if it is a lambda and if so, what sort.
    /// </summary>
    [DebuggerDisplay("{LogicalName}")]
    internal class LambdaArtifact
    {
        /// <summary>
        /// The lambda resource
        /// </summary>
        private readonly ITemplateResource lambdaResource;

        /// <summary>
        /// The path resolver
        /// </summary>
        private readonly IPathResolver pathResolver;

        /// <summary>
        /// The template path
        /// </summary>
        private readonly string templatePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaArtifact"/> class.
        /// </summary>
        /// <param name="pathResolver">The path resolver.</param>
        /// <param name="lambdaResource">The lambda resource.</param>
        /// <param name="templatePath">Path to the template being processed.</param>
        public LambdaArtifact(IPathResolver pathResolver, ITemplateResource lambdaResource, string templatePath)
        {
            this.templatePath = templatePath;
            this.pathResolver = pathResolver;
            this.lambdaResource = lambdaResource;

            switch (lambdaResource.ResourceType)
            {
                case "AWS::Lambda::Function":

                    if (this.HasFileSystemReference("Code"))
                    {
                        break;
                    }

                    this.InlineCode = this.GetResourcePropertyValue("Code.ZipFile");

                    if (this.InlineCode != null)
                    {
                        this.ArtifactType = LambdaArtifactType.Inline;
                        break;
                    }

                    // At this point. either S3 or invalid
                    this.ParseS3Location();
                    break;

                case "AWS::Serverless::Function":

                    // CodeUri or InlineCode
                    this.InlineCode = this.GetResourcePropertyValue("InlineCode");

                    if (this.InlineCode != null)
                    {
                        this.ArtifactType = LambdaArtifactType.Inline;
                        break;
                    }

                    if (this.HasFileSystemReference("CodeUri"))
                    {
                        break;
                    }

                    // At this point. either S3 or invalid
                    this.ParseS3Location();
                    break;

                default:

                    this.ArtifactType = LambdaArtifactType.NotLambda;
                    return;
            }

            // Now get handler and runtime  info
            this.HandlerInfo = new LambdaHandlerInfo(this.lambdaResource);
            this.RuntimeInfo = new LambdaRuntimeInfo(this.lambdaResource);
        }

        /// <summary>
        /// Gets the type of the artifact.
        /// </summary>
        /// <value>
        /// The type of the artifact.
        /// </value>
        public LambdaArtifactType ArtifactType { get; private set; }

        /// <summary>
        /// Gets the containing directory.
        /// </summary>
        /// <value>
        /// The directory containing the local lambda code; else <c>null</c> if lambda not local.
        /// </value>
        public string ContainingDirectory
        {
            get
            {
                switch (this.ArtifactType)
                {
                    case LambdaArtifactType.Directory:

                        return this.Path;

                    case LambdaArtifactType.CodeFile:

                        return System.IO.Path.GetDirectoryName(this.Path);

                    default:

                        return null;
                }
            }
        }

        /// <summary>
        /// Gets the handler information.
        /// </summary>
        /// <value>
        /// The handler information.
        /// </value>
        public LambdaHandlerInfo HandlerInfo { get; }

        /// <summary>
        /// Gets the body of the lambda code, if it is an inline lambda.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public string InlineCode { get; }

        /// <summary>
        /// Gets the resource logical name.
        /// </summary>
        /// <value>
        /// The name of the logical.
        /// </value>
        public string LogicalName => this.lambdaResource.LogicalName;

        /// <summary>
        /// Gets the path to the lambda code, if present in the file system.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the runtime information.
        /// </summary>
        /// <value>
        /// The runtime information.
        /// </value>
        public LambdaRuntimeInfo RuntimeInfo { get; }

        /// <summary>
        /// Gets the location of the code package in S3 if the artifact is remote.
        /// </summary>
        /// <value>
        /// The s3 location.
        /// </value>
        public S3Artifact S3Location { get; private set; }

        /// <summary>
        /// Performs an implicit conversion from <see cref="LambdaArtifact"/> to <see cref="FileInfo"/>.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <exception cref="InvalidCastException">Cannot cast LambdaArtifact of type {self.ArtifactType} to FileInfo</exception>
        public static implicit operator FileInfo(LambdaArtifact self)
        {
            if (self.ArtifactType == LambdaArtifactType.CodeFile || self.ArtifactType == LambdaArtifactType.ZipFile)
            {
                return new FileInfo(self.Path);
            }

            throw new InvalidCastException($"Cannot cast LambdaArtifact of type {self.ArtifactType} to FileInfo");
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="LambdaArtifact"/> to <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <exception cref="InvalidCastException">Cannot cast LambdaArtifact of type {self.ArtifactType} to DirectoryInfo</exception>
        public static implicit operator DirectoryInfo(LambdaArtifact self)
        {
            if (self.ArtifactType == LambdaArtifactType.Directory)
            {
                return new DirectoryInfo(self.Path);
            }

            throw new InvalidCastException($"Cannot cast LambdaArtifact of type {self.ArtifactType} to DirectoryInfo");
        }

        /// <summary>
        /// Load and deserialize this lambda's dependency file
        /// </summary>
        /// <returns>Deserialized Dependencies</returns>
        public List<LambdaDependency> LoadDependencies()
        {
            var dependencyFile = this.GetDependencyFile();

            if (dependencyFile == null)
            {
                return new List<LambdaDependency>();
            }

            // Ensure input file path is absolute
            dependencyFile = System.IO.Path.GetFullPath(dependencyFile);
            var content = File.ReadAllText(dependencyFile).Trim();

            // Determine if JSON
            var firstChar = content.Substring(0, 1);

            if (firstChar == "{")
            {
                // We are expecting an array, not an object
                throw new PackagerException($"{dependencyFile} contains a JSON object. Expecting array");
            }

            try
            {
                var dependencies = firstChar == "["
                                       ? JsonConvert.DeserializeObject<List<LambdaDependency>>(content)
                                       : new YamlDotNet.Serialization.Deserializer()
                                           .Deserialize<List<LambdaDependency>>(content);

                // Make dependency locations absolute
                return dependencies.Select(d => d.ResolveDependencyLocation(dependencyFile)).ToList();
            }
            catch (Exception e)
            {
                // Look for DirectoryNotFoundException raised by LambdaDependency setter to reduce stack trace
                var resolvedException = e;

                var dirException = e.FindInner<DirectoryNotFoundException>();

                if (dirException != null)
                {
                    resolvedException = dirException;
                }

                throw new PackagerException(
                    $"Error deserializing {dependencyFile}: {resolvedException.Message}",
                    resolvedException);
            }
        }

        /// <summary>
        /// Gets the dependency file for this lambda.
        /// </summary>
        /// <returns>Path to dependency file if present; else <c>null</c></returns>
        private string GetDependencyFile()
        {
            if (this.ContainingDirectory == null)
            {
                return null;
            }

            return Directory.GetFiles(this.ContainingDirectory, "lambda-dependencies.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(
                    f =>
                        {
                            var ext = System.IO.Path.GetExtension(f);
                            return string.Compare(ext, ".json", StringComparison.OrdinalIgnoreCase) == 0
                                   || string.Compare(ext, ".yaml", StringComparison.OrdinalIgnoreCase) == 0;
                        });
        }

        /// <summary>
        /// Gets the resource property value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The scalar value, or <c>null</c> if not found or not scalar.</returns>
        private string GetResourcePropertyValue(string property)
        {
            try
            {
                return this.lambdaResource.GetResourcePropertyValue(property);
            }
            catch (FormatException)
            {
                // We don't cae if the property doesn't exist
                return null;
            }
        }

        /// <summary>
        /// Determines whether the specified property name contains a file system reference, and sets properties if it has.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>
        ///   <c>true</c> if [has file system reference] [the specified property name]; otherwise, <c>false</c>.
        /// </returns>
        private bool HasFileSystemReference(string propertyName)
        {
            var fsi = PackagerUtils.ResolveFileSystemResource(
                this.pathResolver,
                this.templatePath,
                this.GetResourcePropertyValue(propertyName));

            if (fsi == null)
            {
                return false;
            }

            // File or directory
            this.Path = fsi.FullName;

            this.ArtifactType = fsi is FileInfo fi
                                    ? System.IO.Path.GetExtension(fi.Name).ToLowerInvariant() == ".zip"
                                          ?
                                          LambdaArtifactType.ZipFile
                                          : LambdaArtifactType.CodeFile
                                    : LambdaArtifactType.Directory;

            return true;
        }

        /// <summary>
        /// Parses the s3 location.
        /// </summary>
        /// <exception cref="PackagerException">Invalid or missing {(isServerless ? "CodeUri" : "Code")} property for {this.lambdaResource.LogicalName}</exception>
        private void ParseS3Location()
        {
            var isServerless = this.lambdaResource.ResourceType == "AWS::Serverless::Function";

            var bucket = this.GetResourcePropertyValue(isServerless ? "CodeUri.Bucket" : "Code.S3Bucket");
            var key = this.GetResourcePropertyValue(isServerless ? "CodeUri.Key" : "Code.S3Key");
            var version = this.GetResourcePropertyValue(isServerless ? "CodeUri.Version" : "Code.S3ObjectVersion");

            if (bucket == null || key == null)
            {
                throw new PackagerException(
                    $"Invalid or missing {(isServerless ? "CodeUri" : "Code")} property for {this.lambdaResource.LogicalName}");
            }

            this.ArtifactType = LambdaArtifactType.FromS3;
            this.S3Location = new S3Artifact { BucketName = bucket, Key = key, ObjectVersion = version };
        }
    }
}