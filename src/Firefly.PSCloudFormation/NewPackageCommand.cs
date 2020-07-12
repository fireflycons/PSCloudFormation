namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Reflection;
    using System.Threading.Tasks;

    using Firefly.CloudFormation.CloudFormation.Parsers;
    using Firefly.CloudFormation.S3;
    using Firefly.CrossPlatformZip;

    /// <summary>
    /// <para type="synopsis">
    /// Packages the local artifacts (local paths) that your AWS CloudFormation template references.
    /// </para>
    /// <para type="description">
    /// The command uploads local artifacts, such as source code for an AWS Lambda function or a Swagger file for an AWS API Gateway REST API, to an S3 bucket.
    /// The command returns a copy of your template, replacing references to local artifacts with the S3 location where the command uploaded the artifacts.
    /// </para>
    /// <para type="description">
    /// Use this command to quickly upload local artifacts that might be required by your template.
    /// After you package your template's artifacts, run the <c>New-PSCFNStack</c> command to deploy the returned template.
    /// </para>
    /// <para type="description">
    /// To specify a local artifact in your template, specify a path to a local file or folder, as either an absolute or relative path.
    /// The relative path is a location that is relative to your template's location.
    /// </para>
    /// <para type="description">
    /// For example, if your AWS Lambda function source code is in the <c>/home/user/code/lambdafunction/</c> folder,
    /// specify <c>CodeUri: /home/user/code/lambdafunction</c> for the <c>AWS::Serverless::Function</c> resource.
    /// The command returns a template and replaces the local path with the S3 location: <c>CodeUri: s3://mybucket/lambdafunction.zip</c>.
    /// </para>
    /// <para type="description">
    /// If you specify a file, the command directly uploads it to the S3 bucket.
    /// If you specify a folder, the command zips the folder and then uploads the .zip file.
    /// For most resources, if you don't specify a path, the command zips and uploads the current working directory.
    /// he exception is <c>AWS::ApiGateway::RestApi</c>; if you don't specify a <c>BodyS3Location</c>, this command will not upload an artifact to S3.
    /// </para>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.CloudFormationServiceCommand" />
    [Cmdlet(VerbsCommon.New, "PSCFNPackage1")]
    public class NewPackageCommand : CloudFormationServiceCommand
    {
        /// <summary>
        /// Type name for nested stacks (we use this a fair bit)
        /// </summary>
        private const string CloudFormationStack = "AWS::CloudFormation::Stack";

        /// <summary>
        /// Random number generator for getting random alphanumeric strings
        /// </summary>
        private static readonly Random Random = new Random((int)(DateTime.Now.Ticks & 0x7fffffff));

        /// <summary>
        /// Map of resources that can be packaged to the properties that should be examined.
        /// </summary>
        private static readonly Dictionary<string, List<PackagedResourceProperties>> PackagedResources =
            new Dictionary<string, List<PackagedResourceProperties>>
                {
                    {
                        "AWS::ApiGateway::RestApi",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false, PropertyPath = "BodyS3Location", ReplacementType = typeof(string)
                                    }
                            }
                    },
                    {
                        "AWS::Lambda::Function",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = true, PropertyPath = "Code", ReplacementType = typeof(S3LocationLong)
                                    }
                            }
                    },
                    {
                        "AWS::Serverless::Function",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = true, PropertyPath = "CodeUri", ReplacementType = typeof(S3LocationShort)
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
                                        ReplacementType = typeof(string)
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
                                        ReplacementType = typeof(string)
                                    },
                                new PackagedResourceProperties
                                    {
                                        Zip = false,
                                        PropertyPath = "ResponseMappingTemplateS3Location",
                                        ReplacementType = typeof(string)
                                    },
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
                                        ReplacementType = typeof(S3LocationShort)
                                    }
                            }
                    },
                    {
                        "AWS::Include",
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false, PropertyPath = "Location", ReplacementType = typeof(string)
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
                                        ReplacementType = typeof(S3LocationLong)
                                    }
                            }
                    },
                    {
                        CloudFormationStack,
                        new List<PackagedResourceProperties>
                            {
                                new PackagedResourceProperties
                                    {
                                        Zip = false, PropertyPath = "TemplateURL", ReplacementType = typeof(string)
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
                                        ReplacementType = typeof(string)
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
                                        ReplacementType = typeof(S3LocationShort)
                                    }
                            }
                    }
                };

        /// <summary>
        /// The bucket operations instance for S3 uploads
        /// </summary>
        internal CloudFormationBucketOperations BucketOperations { get; set; }

        /// <summary>
        /// Instance of PowerShell implementation of path resolver using provider intrinsic
        /// </summary>
        internal IPathResolver PathResolver { get; set; }

        /// <summary>
        /// The output template file
        /// </summary>
        private string outputTemplateFile;

        /// <summary>
        /// The template file
        /// </summary>
        private string templateFile;

        /// <summary>
        /// Gets or sets the metadata.
        /// <para type="description">
        /// A map of metadata to attach to ALL the artifacts that are referenced in your template.
        /// </para>
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public IDictionary Metadata { get; set; }

        /// <summary>
        /// Gets or sets the output template file.
        /// <para type="description">
        /// The path to the file where the command writes the output AWS CloudFormation template. If you don't specify a path, the command writes the template to the standard output.
        /// </para>
        /// </summary>
        /// <value>
        /// The output template file.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string OutputTemplateFile
        {
            get => this.outputTemplateFile;
            set => this.outputTemplateFile = this.ResolvePath(value);
        }

        /// <summary>
        /// Gets or sets the s3 bucket.
        /// <para type="description">
        /// The name of the S3 bucket where this command uploads the artifacts that are referenced in your template.
        /// If not specified, then the oversize template bucket will be used.
        /// </para>
        /// </summary>
        /// <value>
        /// The s3 bucket.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Bucket", "BucketName")]
        public string S3Bucket { get; set; }

        /// <summary>
        /// Gets or sets the s3 prefix.
        /// <para type="description">
        /// A prefix name that the command adds to the artifacts' name when it uploads them to the S3 bucket. The prefix name is a path name (folder name) for the S3 bucket.
        /// </para>
        /// </summary>
        /// <value>
        /// The s3 prefix.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Prefix", "KeyPrefix")]
        public string S3Prefix { get; set; }

        /// <summary>
        /// Gets or sets the template file.
        /// <para type="description">
        /// The path where your AWS CloudFormation template is located.
        /// </para>
        /// </summary>
        /// <value>
        /// The template file.
        /// </value>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        public string TemplateFile
        {
            get => this.templateFile;
            set => this.templateFile = this.ResolvePath(value);
        }

        /// <summary>
        /// Processes template and nested templates recursively, leaf first
        /// </summary>
        /// <param name="templatePath">The template path.</param>
        /// <returns>The path to the processed template.</returns>
        internal async Task<string> ProcessTemplate(string templatePath)
        {
            var outputTemplatePath = templatePath;
            var parser = TemplateParser.Create(File.ReadAllText(templatePath));
            var resources = parser.GetResources().ToList();
            var isModified = false;
            var keyPrefix = this.S3Prefix != null ? this.S3Prefix.TrimEnd('/') : string.Empty;

            // Process nested stacks first
            foreach (var nestedStack in resources.Where(r => r.ResourceType == CloudFormationStack))
            {
                var nestedTemplateLocation = this.ResolveFileSystemResource(
                    templatePath,
                    nestedStack.GetResourcePropertyValue("TemplateURL"));

                switch (nestedTemplateLocation)
                {
                    case null:

                        // Value of TemplateURL is already a URL.
                        continue;

                    case FileInfo fi:

                        // Referenced nested template is in the filesystem, therefore it must be uploaded
                        // whether or not it was itself modified
                        var processedTemplate = await this.ProcessTemplate(fi.FullName);
                        isModified = true;

                        // Output intermediate templates to console if -Debug
                        this.Logger.LogDebug(
                            "Processed template '{0}'\n\n{1}",
                            fi.FullName,
                            File.ReadAllText(processedTemplate));

                        var key = keyPrefix + Path.GetFileName(processedTemplate);

                        this.Logger.LogVerbose(
                            $"Uploading processed '{fi.FullName}' to s3://{this.BucketOperations.BucketName}/{key}");
                        var uri = await this.BucketOperations.UploadFileToS3(processedTemplate, key);

                        // Update resource to point to uploaded template
                        nestedStack.UpdateResourceProperty("TemplateURL", uri.AbsoluteUri);
                        break;

                    default:

                        // The path references a directory, which is illegal in this context.
                        throw new FileNotFoundException(
                            $"Nested stack resource '{nestedStack.LogicalName}': TemplateURL cannot refer to a directory.");
                }
            }

            // Process remaining resources
            foreach (var resource in resources.Where(
                r => r.ResourceType != CloudFormationStack && PackagedResources.ContainsKey(r.ResourceType)))
            {
                foreach (var propertyToCheck in PackagedResources[resource.ResourceType])
                {
                    var resourceFile = resource.GetResourcePropertyValue(propertyToCheck.PropertyPath);

                    if (resourceFile == null)
                    {
                        // Property was not found, or was not a value type.
                        continue;
                    }

                    var fsi = this.ResolveFileSystemResource(templatePath, resourceFile);

                    if (fsi == null)
                    {
                        // Property value did not resolve to a path in the file system
                        continue;
                    }

                    string fileToUpload = null;
                    isModified = true;

                    switch (fsi)
                    {
                        case FileInfo fi:

                            // Property value points to a file
                            if (propertyToCheck.Zip)
                            {
                                fileToUpload = Path.Combine(
                                    Path.GetTempPath(),
                                    $"{Path.GetFileNameWithoutExtension(fi.Name)}-{RandomString(12)}.zip");

                                this.Logger.LogVerbose($"Zipping '{fi.FullName}' to {Path.GetFileName(fileToUpload)}");
                                
                                Zipper.Zip(
                                    new CrossPlatformZipSettings
                                        {
                                            Artifacts = fi.FullName,
                                            CompressionLevel = 9,
                                            LogMessage = (m) => this.Logger.LogVerbose(m),
                                            LogError = (e) => this.Logger.LogError(e),
                                            ZipFile = fileToUpload,
                                            TargetPlatform = ZipPlatform.Unix
                                        });
                            }
                            else
                            {
                                fileToUpload = fi.FullName;
                            }

                            break;

                        case DirectoryInfo di:

                            // Property value points to a directory, which must always be zipped.
                            fileToUpload = Path.Combine(Path.GetTempPath(), $"{di.Name}-{RandomString(12)}.zip");
                            
                            this.Logger.LogVerbose($"Zipping directory '{di.FullName}' to {Path.GetFileName(fileToUpload)}");

                            Zipper.Zip(
                                new CrossPlatformZipSettings
                                    {
                                        Artifacts = di.FullName,
                                        CompressionLevel = 9,
                                        LogMessage = (m) => this.Logger.LogVerbose(m),
                                        LogError = (e) => this.Logger.LogError(e),
                                        ZipFile = fileToUpload,
                                        TargetPlatform = ZipPlatform.Unix
                                    });

                            break;
                    }

                    var key = keyPrefix + Path.GetFileName(fileToUpload);

                    this.Logger.LogVerbose(
                        $"Uploading processed '{fsi.FullName}' to s3://{this.BucketOperations.BucketName}/{key}");
                    var uri = await this.BucketOperations.UploadFileToS3(fileToUpload, key);

                    if (propertyToCheck.ReplacementType == typeof(string))
                    {
                        // Insert the URI directly
                        resource.UpdateResourceProperty(propertyToCheck.PropertyPath, uri.AbsoluteUri);
                    }
                    else
                    {
                        // Create an instance of the new mapping
                        // ReSharper disable once StyleCop.SA1305
                        var s3Location = propertyToCheck.ReplacementType.GetConstructor(new[] { typeof(Uri) })
                            ?.Invoke(new object[] { uri });

                        if (s3Location == null)
                        {
                            throw new MissingMethodException(propertyToCheck.ReplacementType.FullName, ".ctor(Uri)");
                        }

                        // and set on the resource property
                        resource.UpdateResourceProperty(propertyToCheck.PropertyPath, s3Location);
                    }
                }
            }

            if (isModified)
            {
                // Generate a filename and save.
                // The caller will upload the template to S3
                outputTemplatePath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Path.GetFileNameWithoutExtension(templatePath)}-{RandomString(12)}{Path.GetExtension(templatePath)}");

                parser.Save(outputTemplatePath);
            }

            return outputTemplatePath;
        }

        /// <summary>
        /// Processes the record.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            this.PathResolver = new PSPathResolver(this.SessionState);

            var context = this.CreateCloudFormationContext();
            var clientFactory = new PSAwsClientFactory(
                this.CreateClient(this._CurrentCredentials, this._RegionEndpoint),
                context);
            this.BucketOperations = new CloudFormationBucketOperations(clientFactory, context, this.S3Bucket);
            var processedTemplatePath = this.ProcessTemplate(this.TemplateFile).Result;

            // The base stack template was changed
            if (this.OutputTemplateFile != null)
            {
                File.Copy(processedTemplatePath, this.OutputTemplateFile, true);
            }
            else
            {
                this.WriteObject(File.ReadAllText(processedTemplatePath));
            }

            if (processedTemplatePath != this.TemplateFile)
            {
                File.Delete(processedTemplatePath);
            }
        }

        /// <summary>
        /// Generate a random alphanumeric string.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>Random string of specified length</returns>
        private static string RandomString(int length)
        {
            const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(Chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Examines the given template property value and determines if it points to something in the file system.
        /// </summary>
        /// <param name="templatePath">Path to the template being processed.</param>
        /// <param name="propertyValue">Value of the resource property in the template</param>
        /// <returns>A <see cref="FileSystemInfo"/> pointing to the referenced file or directory; else <c>null</c> if not a file system resource.</returns>
        /// <exception cref="FileNotFoundException">The artifact looks like a path, but it cannot be found.</exception>
        private FileSystemInfo ResolveFileSystemResource(
            string templatePath,
            string propertyValue)
        {
            if (propertyValue == null)
            {
                return null;
            }

            string fullPath = null;
            var currentLocation = this.PathResolver.GetLocation();

            if (Path.IsPathRooted(propertyValue) || @"\".Equals(Path.GetPathRoot(propertyValue)))
            {
                fullPath = propertyValue;
            }
            else
            {
                try
                {
                    this.PathResolver.SetLocation(Path.GetDirectoryName(templatePath));

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
                        fullPath = this.PathResolver.ResolvePath(propertyValue);
                    }
                }
                finally
                {
                    this.PathResolver.SetLocation(currentLocation);
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
        /// Describes where to find and how to treat a package artifact
        /// </summary>
        private class PackagedResourceProperties
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
            /// Gets or sets a value indicating whether a single file artifact should be zipped
            /// </summary>
            /// <value>
            ///   <c>true</c> if zip; otherwise, <c>false</c>.
            /// </value>
            public bool Zip { get; set; }
        }

        /// <summary>
        /// Base class to provide constructor helper for the two S3 mapping types that can be inserted into template
        /// </summary>
        private abstract class S3Location
        {
            /// <summary>
            /// Decodes the URI.
            /// </summary>
            /// <param name="uri">The URI.</param>
            /// <returns>Tuple of bucket name and key</returns>
            protected static (string, string) DecodeUri(Uri uri)
            {
                return (uri.Host.Split('.').First(), uri.AbsolutePath.TrimStart('/'));
            }
        }

        /// <summary>
        /// S3 location with 'S3' prefixing field names
        /// </summary>
        private class S3LocationLong : S3Location
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="S3LocationLong" /> class.
            /// </summary>
            /// <param name="s3Uri">The s3 URI.</param>

            // ReSharper disable once StyleCop.SA1305
            public S3LocationLong(Uri s3Uri)
            {
                (this.S3Bucket, this.S3Key) = DecodeUri(s3Uri);
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
        private class S3LocationShort : S3Location
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="S3LocationShort" /> class.
            /// </summary>
            /// <param name="s3Uri">The s3 URI.</param>
            public S3LocationShort(Uri s3Uri)
            {
                (this.Bucket, this.Key) = DecodeUri(s3Uri);
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