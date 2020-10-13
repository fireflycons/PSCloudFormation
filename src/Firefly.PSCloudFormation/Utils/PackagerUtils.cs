namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Parsers;

    /// <summary>
    /// Manages the packaging of a template
    /// </summary>
    /// <remarks>
    /// Only local file-based templates can be packaged.
    /// S3 and UsePreviousTemplate are by definition already packaged.
    /// Input String templates have no context in the file system for resolving relative paths.
    /// </remarks>
    internal class PackagerUtils
    {
        /// <summary>
        /// Type name for nested stacks (we use this a fair bit)
        /// </summary>
        public const string CloudFormationStack = "AWS::CloudFormation::Stack";

        /// <summary>
        /// Map of resources that can be packaged to the properties that should be examined.
        /// </summary>
        internal static readonly Dictionary<string, List<PackagedResourceProperties>> PackagedResources =
            new Dictionary<string, List<PackagedResourceProperties>>
                {
                    {
                        "AWS::ApiGateway::RestApi",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "BodyS3Location",
                                        ReplacementType = typeof(string),
                                        Required = false
                                    }
                            }
                    },
                    {
                        "AWS::Lambda::Function",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = true,
                                        PropertyPath = "Code",
                                        ReplacementType = typeof(S3LocationLong),
                                        Required = true
                                    }
                            }
                    },
                    {
                        "AWS::Serverless::Function",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = true,
                                        PropertyPath = "CodeUri",
                                        ReplacementType = typeof(S3LocationShort),
                                        Required = false
                                    }
                            }
                    },
                    {
                        "AWS::AppSync::GraphQLSchema",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "DefinitionS3Location",
                                        ReplacementType = typeof(string),
                                        Required = false
                                    }
                            }
                    },
                    {
                        "AWS::AppSync::Resolver",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "RequestMappingTemplateS3Location",
                                        ReplacementType = typeof(string),
                                        Required = false
                                    },
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "ResponseMappingTemplateS3Location",
                                        ReplacementType = typeof(string),
                                        Required = false
                                    }
                            }
                    },
                    {
                        "AWS::Serverless::Api",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "DefinitionUri",
                                        ReplacementType = typeof(S3LocationShort),
                                        Required = false
                                    }
                            }
                    },
                    {
                        "AWS::Include",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "Location",
                                        ReplacementType = typeof(string),
                                        Required = true
                                    }
                            }
                    },
                    {
                        "AWS::ElasticBeanstalk::ApplicationVersion",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = true,
                                        PropertyPath = "SourceBundle",
                                        ReplacementType = typeof(S3LocationLong),
                                        Required = true
                                    }
                            }
                    },
                    {
                        CloudFormationStack,
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "TemplateURL",
                                        ReplacementType = typeof(string),
                                        Required = true
                                    }
                            }
                    },
                    {
                        "AWS::Glue::Job",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = true,
                                        PropertyPath = "Command.ScriptLocation",
                                        ReplacementType = typeof(string),
                                        Required = true
                                    }
                            }
                    },
                    {
                        "AWS::StepFunctions::StateMachine",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "DefinitionS3Location",
                                        ReplacementType = typeof(S3LocationShort),
                                        Required = false
                                    }
                            }
                    }
                };

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The path resolver
        /// </summary>
        private readonly IPathResolver pathResolver;

        /// <summary>
        /// The s3 utility
        /// </summary>
        // ReSharper disable once StyleCop.SA1305
        private readonly IPSS3Util s3Util;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackagerUtils"/> class.
        /// </summary>
        /// <param name="pathResolver">The path resolver.</param>
        /// <param name="logger">Logging interface.</param>
        /// <param name="s3Util">S3 utility to use for pushing packaged objects.</param>
        // ReSharper disable once StyleCop.SA1305
        public PackagerUtils(IPathResolver pathResolver, ILogger logger, IPSS3Util s3Util)
        {
            this.s3Util = s3Util;
            this.logger = logger;
            this.pathResolver = pathResolver;
        }

        /// <summary>
        /// Examines the given template property value and determines if it points to something in the file system.
        /// </summary>
        /// <param name="pathResolver">The path resolver to use.</param>
        /// <param name="templatePath">Path to the template being processed.</param>
        /// <param name="propertyValue">Value of the resource property in the template</param>
        /// <returns>A <see cref="FileSystemInfo"/> pointing to the referenced file or directory; else <c>null</c> if not a file system resource.</returns>
        /// <exception cref="FileNotFoundException">The artifact looks like a path, but it cannot be found.</exception>
        public static FileSystemInfo ResolveFileSystemResource(
            IPathResolver pathResolver,
            string templatePath,
            string propertyValue)
        {
            if (propertyValue == null)
            {
                return null;
            }

            string fullPath = null;
            var currentLocation = pathResolver.GetLocation();

            if (Path.IsPathRooted(propertyValue) || @"\".Equals(Path.GetPathRoot(propertyValue)))
            {
                fullPath = propertyValue;
            }
            else
            {
                try
                {
                    pathResolver.SetLocation(Path.GetDirectoryName(templatePath));

                    try
                    {
                        if (Uri.TryCreate(propertyValue, UriKind.Absolute, out var uri))
                        {
                            if (!string.IsNullOrEmpty(uri.Scheme))
                            {
                                if (uri.Scheme != "file")
                                {
                                    // It is a real URL and therefore no more to do
                                    return null;
                                }

                                // It is definitely a file and is an absolute path
                                fullPath = propertyValue;
                            }
                        }
                    }
                    catch
                    {
                        // Do nothing
                    }

                    if (fullPath == null)
                    {
                        // Assume we have a relative path, so convert to absolute
                        fullPath = pathResolver.ResolvePath(propertyValue);
                    }
                }
                finally
                {
                    pathResolver.SetLocation(currentLocation);
                }
            }

            // ReSharper disable once InvertIf - would result in duplication of the throw at the end.
            if (fullPath != null)
            {
                if (File.Exists(fullPath))
                {
                    return new FileInfo(fullPath);
                }

                if (Directory.Exists(fullPath))
                {
                    return new DirectoryInfo(fullPath);
                }
            }

            throw new FileNotFoundException($"Path not found: '{propertyValue}");
        }

        /// <summary>
        /// Processes template and nested templates recursively, leaf first
        /// </summary>
        /// <param name="templatePath">The template path.</param>
        /// <param name="workingDirectory">Working directory for files to upload.</param>
        /// <returns>The path to the processed template.</returns>
        public async Task<string> ProcessTemplate(string templatePath, string workingDirectory)
        {
            var outputTemplatePath = templatePath;
            var parser = TemplateParser.Create(File.ReadAllText(templatePath));
            var resources = parser.GetResources().ToList();
            var templateModified = false;

            // Process nested stacks first
            foreach (var nestedStack in resources.Where(r => r.ResourceType == CloudFormationStack))
            {
                templateModified |= await this.ProcessNestedStack(nestedStack, templatePath, workingDirectory);
            }

            // Process remaining resources
            foreach (var resource in resources.Where(
                r => r.ResourceType != CloudFormationStack && PackagedResources.ContainsKey(r.ResourceType)))
            {
                templateModified |= await this.ProcessResource(resource, templatePath, workingDirectory);
            }

            if (templateModified)
            {
                // Generate a filename and save.
                // The caller will upload the template to S3
                outputTemplatePath = Path.Combine(workingDirectory, Path.GetFileName(templatePath));

                parser.Save(outputTemplatePath);
            }

            return outputTemplatePath;
        }

        /// <summary>
        /// <para>
        /// Determine if given template requires packaging, i.e. any resources of the types in <see cref="PackagerUtils.PackagedResources"/>
        /// or any nested templates point to files.
        /// </para>
        /// <para>
        /// Packaging applies only to templates of type file or input string. If the template is remote, then by definition it does not point to local resources.
        /// </para>
        /// </summary>
        /// <param name="templatePath">File system path to template to process.</param>
        /// <returns><c>true</c> if packaging is required; else <c>false</c>.</returns>
        public bool RequiresPackaging(string templatePath)
        {
            if (!File.Exists(this.pathResolver.ResolvePath(templatePath)))
            {
                // Applies to input string templates also
                return false;
            }

            var parser = TemplateParser.Create(File.ReadAllText(templatePath));
            var resources = parser.GetResources().ToList();

            // Any nested stacks return not null for ResolveFileSystemResource point to files and thus need packaging.
            if (resources.Where(r => r.ResourceType == CloudFormationStack).Any(
                nestedStackResource => ResolveFileSystemResource(
                                           this.pathResolver,
                                           templatePath,
                                           nestedStackResource.GetResourcePropertyValue("TemplateURL")) != null))
            {
                return true;
            }

            // Process remaining resources
            foreach (var resource in resources.Where(
                r => r.ResourceType != CloudFormationStack && PackagedResources.ContainsKey(r.ResourceType)))
            {
                foreach (var propertyToCheck in PackagedResources[resource.ResourceType])
                {
                    string resourceFile;

                    try
                    {
                        resourceFile = resource.GetResourcePropertyValue(propertyToCheck.PropertyPath);
                    }
                    catch (FormatException)
                    {
                        if (!propertyToCheck.Required)
                        {
                            // Property is missing, but CloudFormation does not require it.
                            continue;
                        }

                        throw;
                    }

                    if (resourceFile == null)
                    {
                        // Property was not found, or was not a value type.
                        continue;
                    }

                    var fsi = ResolveFileSystemResource(this.pathResolver, templatePath, resourceFile);

                    if (fsi != null)
                    {
                        // Found a file system reference, therefore packaging required.
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Processes a nested stack.
        /// </summary>
        /// <param name="nestedStackResource">The nested stack.</param>
        /// <param name="templatePath">The template path.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns><c>true</c> if the containing template should be modified (to point to S3); else <c>false</c></returns>
        /// <exception cref="FileNotFoundException">Nested stack resource '{nestedStackResource.LogicalName}': TemplateURL cannot refer to a directory.</exception>
        private async Task<bool> ProcessNestedStack(
            TemplateResource nestedStackResource,
            string templatePath,
            string workingDirectory)
        {
            var nestedTemplateLocation = ResolveFileSystemResource(
                this.pathResolver,
                templatePath,
                nestedStackResource.GetResourcePropertyValue("TemplateURL"));

            switch (nestedTemplateLocation)
            {
                case null:

                    // Value of TemplateURL is already a URL.
                    return false;

                case FileInfo fi:

                    // Referenced nested template is in the filesystem, therefore it must be uploaded
                    // whether or not it was itself modified
                    var processedTemplate = new FileInfo(await this.ProcessTemplate(fi.FullName, workingDirectory));
                    var templateHash = processedTemplate.MD5();

                    // Output intermediate templates to console if -Debug
                    this.logger.LogDebug($"Processed template '{fi.FullName}', Hash: {templateHash}");
                    this.logger.LogDebug("\n\n{0}", File.ReadAllText(processedTemplate.FullName));

                    var resourceToUpload = new ResourceUploadSettings
                                               {
                                                   File = processedTemplate,
                                                   Hash = templateHash,
                                                   KeyPrefix = this.s3Util.KeyPrefix,
                                                   Metadata = this.s3Util.Metadata
                                               };

                    if (await this.s3Util.ObjectChangedAsync(resourceToUpload))
                    {
                        await this.s3Util.UploadResourceToS3Async(resourceToUpload);
                    }

                    // Update resource to point to uploaded template
                    nestedStackResource.UpdateResourceProperty("TemplateURL", resourceToUpload.S3Artifact.Url);

                    break;

                default:

                    // The path references a directory, which is illegal in this context.
                    throw new FileNotFoundException(
                        $"Nested stack resource '{nestedStackResource.LogicalName}': TemplateURL cannot refer to a directory.");
            }

            return true;
        }

        /// <summary>
        /// Processes resources that can point to artifacts in S3.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="templatePath">The template path.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns><c>true</c> if the containing template should be modified (to point to S3); else <c>false</c></returns>
        /// <exception cref="InvalidDataException">Unsupported derivative of FileSystemInfo</exception>
        /// <exception cref="MissingMethodException">Missing constructor for the replacement template artifact.</exception>
        private async Task<bool> ProcessResource(
            TemplateResource resource,
            string templatePath,
            string workingDirectory)
        {
            var templateModified = false;

            foreach (var propertyToCheck in PackagedResources[resource.ResourceType])
            {
                ResourceUploadSettings resourceToUpload;

                // See if we have a lambda
                var lambdaResource = new LambdaArtifact(this.pathResolver, templatePath, resource);

                if (lambdaResource.ArtifactType != LambdaArtifactType.NotLambda)
                {
                    // We do
                    using (var packager = LambdaPackager.CreatePackager(lambdaResource, this.s3Util, this.logger))
                    {
                        resourceToUpload = await packager.Package(workingDirectory);

                        if (resourceToUpload == null)
                        {
                            // Lambda syntax does not imply a template modification
                            // i.e. it is inline code or already an S3 reference.
                            continue;
                        }

                        // The template will be altered to an S3 location,
                        // however the zip may or may not be uploaded.
                        templateModified = true;
                    }
                }
                else
                {
                    string resourceFile;

                    try
                    {
                        resourceFile = resource.GetResourcePropertyValue(propertyToCheck.PropertyPath);
                    }
                    catch (FormatException)
                    {
                        if (!propertyToCheck.Required)
                        {
                            // Property is missing, but CloudFormation does not require it.
                            continue;
                        }

                        throw;
                    }

                    if (resourceFile == null)
                    {
                        // Property was not found, or was not a value type.
                        continue;
                    }

                    var fsi = ResolveFileSystemResource(this.pathResolver, templatePath, resourceFile);

                    if (fsi == null)
                    {
                        // Property value did not resolve to a path in the file system
                        continue;
                    }

                    templateModified = true;
                    switch (fsi)
                    {
                        case FileInfo fi:

                            // Property value points to a file
                            resourceToUpload = await ArtifactPackager.PackageFile(
                                                   fi,
                                                   workingDirectory,
                                                   propertyToCheck.Zip,
                                                   this.s3Util,
                                                   this.logger);
                            break;

                        case DirectoryInfo di:

                            // Property value points to a directory, which must always be zipped.
                            resourceToUpload = await ArtifactPackager.PackageDirectory(
                                                   di,
                                                   workingDirectory,
                                                   this.s3Util,
                                                   this.logger);
                            break;

                        default:

                            // Should never get here, but shuts up a bunch of compiler/R# warnings
                            throw new InvalidDataException(
                                $"Unsupported derivative of FileSystemInfo: {fsi.GetType().FullName}");
                    }
                }

                if (!resourceToUpload.HashMatch)
                {
                    resourceToUpload.KeyPrefix = this.s3Util.KeyPrefix;
                    resourceToUpload.Metadata = this.s3Util.Metadata;

                    await this.s3Util.UploadResourceToS3Async(resourceToUpload);
                }

                if (propertyToCheck.ReplacementType == typeof(string))
                {
                    // Insert the URI directly
                    resource.UpdateResourceProperty(propertyToCheck.PropertyPath, resourceToUpload.S3Artifact.Url);
                }
                else
                {
                    // Create an instance of the new mapping
                    // ReSharper disable once StyleCop.SA1305
                    var s3Location = propertyToCheck.ReplacementType.GetConstructor(new[] { typeof(S3Artifact) })
                        ?.Invoke(new object[] { resourceToUpload.S3Artifact });

                    if (s3Location == null)
                    {
                        throw new MissingMethodException(propertyToCheck.ReplacementType.FullName, ".ctor(S3Artifact)");
                    }

                    // and set on the resource property
                    resource.UpdateResourceProperty(propertyToCheck.PropertyPath, s3Location);
                }
            }

            return templateModified;
        }

        /// <summary>
        /// Describes where to find and how to treat a package artifact
        /// </summary>
        internal class PackagedResourceProperties
        {
            /// <summary>
            /// Gets or sets path to the property to examine within resource's Properties section
            /// </summary>
            /// <value>
            /// The property path.
            /// </value>
            public string PropertyPath { get; set; }

            /// <summary>
            /// Gets or sets the type of object that will replace the resource property containing the file name.
            /// Where this is <see cref="string"/>, then it is a simple value replacement with an S3 URI
            /// </summary>
            public Type ReplacementType { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="PackagedResourceProperties"/> is required.
            /// Indicates whether the property is required for the resource to be valid
            /// </summary>
            /// <value>
            ///   <c>true</c> if required; otherwise, <c>false</c>.
            /// </value>
            public bool Required { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether a single file artifact should be zipped
            /// </summary>
            /// <value>
            ///   <c>true</c> if zip; otherwise, <c>false</c>.
            /// </value>
            public bool Zip { get; set; }
        }

        /// <summary>
        /// S3 location with 'S3' prefixing field names
        /// </summary>
        // ReSharper disable UnusedAutoPropertyAccessor.Local - Classes are accessed by refection
        // ReSharper disable MemberCanBePrivate.Local
        private class S3LocationLong
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="S3LocationLong" /> class.
            /// </summary>
            /// <param name="artifact">The S3 artifact.</param>
            // ReSharper disable once StyleCop.SA1305
            public S3LocationLong(S3Artifact artifact)
            {
                this.S3Bucket = artifact.BucketName;
                this.S3Key = artifact.Key;
            }

            /// <summary>
            /// Gets the s3 bucket.
            /// </summary>
            /// <value>
            /// The s3 bucket.
            /// </value>
            public string S3Bucket { get; }

            /// <summary>
            /// Gets the s3 key.
            /// </summary>
            /// <value>
            /// The s3 key.
            /// </value>
            public string S3Key { get; }
        }

        /// <summary>
        /// S3 location without 'S3' prefixing field names
        /// </summary>
        private class S3LocationShort
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="S3LocationShort" /> class.
            /// </summary>
            /// <param name="artifact">The s3 URI.</param>
            public S3LocationShort(S3Artifact artifact)
            {
                this.Bucket = artifact.BucketName;
                this.Key = artifact.Key;
            }

            /// <summary>
            /// Gets the bucket.
            /// </summary>
            /// <value>
            /// The bucket.
            /// </value>
            public string Bucket { get; }

            /// <summary>
            /// Gets the key.
            /// </summary>
            /// <value>
            /// The key.
            /// </value>
            public string Key { get; }
        }
    }
}