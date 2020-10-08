namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Firefly.CloudFormation.Parsers;

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

        private readonly IPathResolver pathResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackagerUtils"/> class.
        /// </summary>
        /// <param name="pathResolver">The path resolver.</param>
        public PackagerUtils(IPathResolver pathResolver)
        {
            this.pathResolver = pathResolver;
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
        /// <param name="templateContent">The template file content.</param>
        /// <param name="templatePath">The template path if known; else <c>null</c>.</param>
        /// <returns><c>true</c> if packaging is required; else <c>false</c>.</returns>
        public bool RequiresPackaging(string templateContent, string templatePath)
        {
            var parser = TemplateParser.Create(templateContent);
            var resources = parser.GetResources().ToList();

            templatePath = templatePath ?? this.pathResolver.GetLocation();

            // Any nested stacks return not null for ResolveFileSystemResource point to files and thus need packaging.
            if (resources.Where(r => r.ResourceType == CloudFormationStack).Any(
                nestedStackResource => this.ResolveFileSystemResource(
                                           templatePath,
                                           nestedStackResource.GetResourcePropertyValue("TemplateURL")) != null))
            {
                return true;
            }

            // Process remaining resources
            foreach (var resource in resources.Where(
                r => r.ResourceType != PackagerUtils.CloudFormationStack
                     && PackagerUtils.PackagedResources.ContainsKey(r.ResourceType)))
            {
                foreach (var propertyToCheck in PackagerUtils.PackagedResources[resource.ResourceType])
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

                    var fsi = this.ResolveFileSystemResource(templatePath, resourceFile);

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
        /// Examines the given template property value and determines if it points to something in the file system.
        /// </summary>
        /// <param name="templatePath">Path to the template being processed.</param>
        /// <param name="propertyValue">Value of the resource property in the template</param>
        /// <returns>A <see cref="FileSystemInfo"/> pointing to the referenced file or directory; else <c>null</c> if not a file system resource.</returns>
        /// <exception cref="FileNotFoundException">The artifact looks like a path, but it cannot be found.</exception>
        public FileSystemInfo ResolveFileSystemResource(string templatePath, string propertyValue)
        {
            if (propertyValue == null)
            {
                return null;
            }

            string fullPath = null;
            var currentLocation = this.pathResolver.GetLocation();

            if (Path.IsPathRooted(propertyValue) || @"\".Equals(Path.GetPathRoot(propertyValue)))
            {
                fullPath = propertyValue;
            }
            else
            {
                try
                {
                    this.pathResolver.SetLocation(Path.GetDirectoryName(templatePath));

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
                        fullPath = this.pathResolver.ResolvePath(propertyValue);
                    }
                }
                finally
                {
                    this.pathResolver.SetLocation(currentLocation);
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