namespace Firefly.CloudFormation.Resolvers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormation.S3;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Abstract file resolver class for resolving template or policy content from file, S3 or string body
    /// </summary>
    /// <seealso cref="IInputFileResolver" />
    public abstract class AbstractFileResolver : IInputFileResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractFileResolver"/> class.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        /// <param name="context">The context.</param>
        protected AbstractFileResolver(IAwsClientFactory clientFactory, ICloudFormationContext context)
        {
            this.Context = context;
            this.ClientFactory = clientFactory;
        }

        /// <summary>
        /// Gets or sets the artifact content, used to populate Body properties of stack request objects.
        /// </summary>
        /// <value>
        /// The artifact content, which will be <c>null</c> if the artifact is located in S3 or Use Previous Template is selected for updates.
        /// </value>
        public string ArtifactContent =>
            (this.Source & (InputFileSource.S3 | InputFileSource.UsePreviousTemplate)) != 0 ? null : this.FileContent;

        /// <summary>
        /// Gets the file URL, used to populate URL properties of stack request objects.
        /// </summary>
        /// <value>
        /// The file URL, which will be <c>null</c> if the file is not located in S3.
        /// </value>
        // ReSharper disable once StyleCop.SA1623
        public string ArtifactUrl { get; protected set; }

        /// <summary>
        /// Gets the file content - wherever it is located.
        /// </summary>
        /// <value>
        /// The file content.
        /// </value>

        // ReSharper disable once StyleCop.SA1623
        public string FileContent { get; protected set; }

        /// <summary>
        /// Gets the name of the input file, or "RawString" if the input was a string rather than a file.
        /// </summary>
        /// <value>
        /// The name of the input file.
        /// </value>
        public string InputFileName { get; private set; }

        /// <summary>
        /// Gets the file's source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        // ReSharper disable once StyleCop.SA1623
        public InputFileSource Source { get; protected set; }

        /// <summary>
        /// Gets the client factory.
        /// </summary>
        /// <value>
        /// The client factory.
        /// </value>
        protected IAwsClientFactory ClientFactory { get; }

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        protected ICloudFormationContext Context { get; }

        /// <summary>
        /// Gets the maximum size of the file.
        /// If the file is on local file system and is larger than this number of bytes, it must first be uploaded to S3.
        /// </summary>
        /// <value>
        /// The maximum size of the file.
        /// </value>
        protected abstract int MaxFileSize { get; }

        /// <summary>
        /// Resets the template source if an oversize asset was uploaded to S3.
        /// </summary>
        /// <param name="uploadedArtifactUri">The uploaded artifact URI.</param>
        public void ResetTemplateSource(string uploadedArtifactUri)
        {
            this.Source = InputFileSource.S3;
            this.ArtifactUrl = uploadedArtifactUri;
        }

        /// <summary>
        /// Resolves the given artifact location (template or policy) from text input
        /// uploading it to S3 if the object is larger than the maximum size for
        /// body text supported by the CloudFormation API.
        /// </summary>
        /// <param name="context">The context for logging.</param>
        /// <param name="objectToResolve">The object to resolve.</param>
        /// <param name="stackName">Name of the stack.</param>
        /// <returns>
        /// Result of the resolution.
        /// </returns>
        public async Task<ResolutionResult> ResolveArtifactLocationAsync(
            ICloudFormationContext context,
            string objectToResolve,
            string stackName)
        {
            var result = new ResolutionResult();

            // Nasty, but really want out arguments here.
            this.ResolveFileAsync(objectToResolve).Wait();

            var fileType = this is TemplateResolver ? UploadFileType.Template : UploadFileType.Policy;

            if ((this.Source & InputFileSource.Oversize) != 0)
            {
                if (context.S3Util == null)
                {
                    throw new StackOperationException(
                        $"Unable to upload oversize {fileType.ToString().ToLowerInvariant()} to S3. No implementation of {typeof(IS3Util).FullName} has been provided.");
                }

                result.ArtifactUrl = (await context.S3Util.UploadOversizeArtifactToS3(
                                          stackName,
                                          this.ArtifactContent,
                                          this.InputFileName,
                                          fileType)).AbsoluteUri;

                this.ResetTemplateSource(result.ArtifactUrl);
            }
            else
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (this.Source)
                {
                    case InputFileSource.S3:

                        result.ArtifactUrl = this.ArtifactUrl;
                        break;

                    case InputFileSource.File:
                    case InputFileSource.String:

                        result.ArtifactBody = this.ArtifactContent;
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves and loads the given file from the specified location
        /// </summary>
        /// <param name="objectLocation">The file location.</param>
        /// <returns>
        /// The file content
        /// </returns>
        /// <exception cref="ArgumentException">
        /// 'Path' style S3 URLs must have at least 2 path segments (bucket_name/key)
        /// or
        /// 'Virtual Host' style S3 URLs must have at least 1 path segment (key)
        /// </exception>
        public virtual async Task<string> ResolveFileAsync(string objectLocation)
        {
            if (string.IsNullOrEmpty(objectLocation))
            {
                this.Source = InputFileSource.None;
                return null;
            }

            if (objectLocation.Contains("\r") || objectLocation.Contains("\n"))
            {
                // Definitely a string
                this.GetStringContent(objectLocation);
            }
            else if (File.Exists(objectLocation))
            {
                this.GetFileContent(objectLocation);
            }
            else if (Uri.TryCreate(objectLocation, UriKind.Absolute, out var uri))
            {
                string bucketName;
                string key;

                switch (uri.Scheme)
                {
                    case "https":

                        // User specified S3 URL
                        if (Regex.IsMatch(uri.Host, @"^s3[\.-]", RegexOptions.IgnoreCase))
                        {
                            // Path style
                            // Min of 2 actual path segments required for path style URI - first segment is always '/'
                            if (uri.Segments.Length < 3)
                            {
                                throw new ArgumentException(
                                    // ReSharper disable once StringLiteralTypo
                                    "'Path' style S3 URLs must have at least 2 path segments (bucketname/key)");
                            }

                            bucketName = uri.Segments[0].TrimEnd('/');
                            key = string.Join(string.Empty, uri.Segments.Skip(1));
                        }
                        else
                        {
                            // VHost style
                            // Min of 1 actual path segments required for path style URI - first segment is always '/'
                            if (uri.Segments.Length < 2)
                            {
                                throw new ArgumentException(
                                    "'Virtual Host' style S3 URLs must have at least 1 path segment (key)");
                            }

                            bucketName = uri.Host.Split('.').First();
                            key = uri.LocalPath.TrimStart('/');
                        }

                        this.InputFileName = Path.GetFileNameWithoutExtension(key);
                        this.FileContent = await this.Context.S3Util.GetS3ObjectContent(bucketName, key);
                        this.Source = InputFileSource.S3;

                        this.ArtifactUrl = uri.AbsoluteUri;

                        break;

                    case "s3":

                        bucketName = uri.Host;
                        key = uri.LocalPath.TrimStart('/');

                        this.FileContent = await this.Context.S3Util.GetS3ObjectContent(bucketName, key);
                        this.Source = InputFileSource.S3;

                        // ReSharper disable once StringLiteralTypo
                        this.ArtifactUrl = $"https://{bucketName}.s3.amazonaws.com/{key}";

                        break;

                    default:

                        throw new ArgumentException($"Unsupported URI scheme '{uri.Scheme}");
                }
            }
            else
            {
                // Value is a string
                this.GetStringContent(objectLocation);
            }

            return this.FileContent;
        }

        /// <summary>
        /// Gets the content of the file.
        /// </summary>
        /// <param name="fileLocation">The file location.</param>
        private void GetFileContent(string fileLocation)
        {
            this.Source = InputFileSource.File;

            this.InputFileName = Path.GetFileNameWithoutExtension(fileLocation);

            // ReSharper disable once AssignNullToNotNullAttribute - Existence is verified by the caller
            if (new FileInfo(fileLocation).Length >= this.MaxFileSize)
            {
                this.Source |= InputFileSource.Oversize;
            }

            using (var sr = new StreamReader(File.OpenRead(fileLocation)))
            {
                this.FileContent = sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Resolves an artifact passed to the command as a raw string, checking its length for being oversize.
        /// </summary>
        /// <param name="stringContent">Content of the string.</param>
        private void GetStringContent(string stringContent)
        {
            this.FileContent = stringContent;
            this.Source = InputFileSource.String;
            this.InputFileName = "RawString";

            if (Encoding.UTF8.GetByteCount(stringContent) >= this.MaxFileSize)
            {
                this.Source |= InputFileSource.Oversize;
            }
        }
    }
}