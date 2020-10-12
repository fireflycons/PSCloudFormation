namespace Firefly.PSCloudFormation
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Firefly.CloudFormation.Parsers;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Given a resource, decide if it is a lambda and if so, what sort.
    /// </summary>
    [DebuggerDisplay("{lambdaResource.LogicalName}")]
    internal class LambdaArtifact
    {
        /// <summary>
        /// The lambda resource
        /// </summary>
        private readonly TemplateResource lambdaResource;

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
        /// <param name="templatePath">Path to the template being processed.</param>
        /// <param name="lambdaResource">The lambda resource.</param>
        public LambdaArtifact(IPathResolver pathResolver, string templatePath, TemplateResource lambdaResource)
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
                    break;
            }

            // Now get handler and runtime  info
            this.HandlerInfo = new LambdaHandlerInfo(this.lambdaResource);
            this.RuntimeInfo = new LambdaRuntimeInfo(this.lambdaResource);
        }

        /// <summary>
        /// Gets the runtime information.
        /// </summary>
        /// <value>
        /// The runtime information.
        /// </value>
        public LambdaRuntimeInfo RuntimeInfo { get;  }

        /// <summary>
        /// Gets the handler information.
        /// </summary>
        /// <value>
        /// The handler information.
        /// </value>
        public LambdaHandlerInfo HandlerInfo { get; }

        /// <summary>
        /// Gets the type of the artifact.
        /// </summary>
        /// <value>
        /// The type of the artifact.
        /// </value>
        public LambdaArtifactType ArtifactType { get; private set; }

        /// <summary>
        /// Gets the body of the lambda code, if it is an inline lambda.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public string InlineCode { get; }

        /// <summary>
        /// Gets the path to the lambda code, if present in the file system.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the location of the code package in S3 if the artifact is remote.
        /// </summary>
        /// <value>
        /// The s3 location.
        /// </value>
        public S3Artifact S3Location { get; private set; }

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
            var fsi = PackagerUtils.ResolveFileSystemResource(this.pathResolver, this.templatePath, this.GetResourcePropertyValue(propertyName));

            if (fsi == null)
            {
                return false;
            }

            // File or directory
            this.Path = fsi.FullName;

            this.ArtifactType = fsi is FileInfo fi
                                    ? (System.IO.Path.GetExtension(fi.Name).ToLowerInvariant() == ".zip"
                                           ? LambdaArtifactType.ZipFile
                                           : LambdaArtifactType.CodeFile)
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

            this.ArtifactType = LambdaArtifactType.Remote;
            this.S3Location = new S3Artifact { BucketName = bucket, Key = key, ObjectVersion = version };
        }
    }
}