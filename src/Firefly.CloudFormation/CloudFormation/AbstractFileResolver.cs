namespace Firefly.CloudFormation.CloudFormation
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Amazon.S3.Model;

    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Abstract file resolver class for resolving template or policy content from file, S3 or string body
    /// </summary>
    /// <seealso cref="Firefly.CloudFormation.CloudFormation.IInputFileResolver" />
    public abstract class AbstractFileResolver : IInputFileResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractFileResolver"/> class.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        protected AbstractFileResolver(IAwsClientFactory clientFactory)
        {
            this.ClientFactory = clientFactory;
        }

        /// <summary>
        /// Gets or sets the artifact content, used to populate Body properties of stack request objects.
        /// </summary>
        /// <value>
        /// The artifact content, which will be <c>null</c> if the artifact is located in S3 or Use Previous Template is selected for updates.
        /// </value>
        public string ArtifactContent => (this.Source & (InputFileSource.S3 | InputFileSource.UsePreviousTemplate)) != 0 ? null : this.FileContent;

        /// <summary>
        /// Gets the file content - wherever it is located.
        /// </summary>
        /// <value>
        /// The file content.
        /// </value>
        // ReSharper disable once StyleCop.SA1623
        public string FileContent { get; protected set; }

        /// <summary>
        /// Gets the file URL, used to populate URL properties of stack request objects.
        /// </summary>
        /// <value>
        /// The file URL, which will be <c>null</c> if the file is not located in S3.
        /// </value>
        // ReSharper disable once StyleCop.SA1623
        public string ArtifactUrl { get; protected set; }

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
        /// Gets the maximum size of the file.
        /// If the file is on local file system and is larger than this number of bytes, it must first be uploaded to S3.
        /// </summary>
        /// <value>
        /// The maximum size of the file.
        /// </value>
        protected abstract int MaxFileSize { get; }

        /// <summary>
        /// Resolves and loads the given file from the specified location
        /// </summary>
        /// <param name="fileLocation">The file location.</param>
        /// <returns>
        /// The file content
        /// </returns>
        /// <exception cref="ArgumentException">
        /// 'Path' style S3 URLs must have at least 2 path segments (bucket_name/key)
        /// or
        /// 'Virtual Host' style S3 URLs must have at least 1 path segment (key)
        /// </exception>
        public virtual async Task<string> ResolveFileAsync(string fileLocation)
        {
            if (string.IsNullOrEmpty(fileLocation))
            {
                this.Source = InputFileSource.None;
                return null;
            }

            if (Uri.TryCreate(fileLocation, UriKind.Absolute, out var uri))
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

                        await this.GetS3ObjectContent(bucketName, key);

                        this.ArtifactUrl = uri.AbsoluteUri;

                        break;

                    case "s3":

                        bucketName = uri.Host;
                        key = uri.LocalPath.TrimStart('/');

                        await this.GetS3ObjectContent(bucketName, key);
                        // ReSharper disable once StringLiteralTypo
                        this.ArtifactUrl = $"https://{bucketName}.s3.amazonaws.com/{key}";

                        break;

                    case "file":

                        // Local file
                        this.Source = InputFileSource.File;

                        if (new FileInfo(fileLocation).Length >= this.MaxFileSize)
                        {
                            this.Source |= InputFileSource.Oversize;
                        }

                        using (var sr = new StreamReader(File.OpenRead(fileLocation)))
                        {
                            this.FileContent = sr.ReadToEnd();
                        }

                        break;

                    default:

                        throw new ArgumentException($"Unsupported URI scheme '{uri.Scheme}");
                }
            }
            else
            {
                // Value is a string
                this.FileContent = fileLocation;
                this.Source = InputFileSource.String;

                if (Encoding.UTF8.GetByteCount(fileLocation) >= this.MaxFileSize)
                {
                    this.Source |= InputFileSource.Oversize;
                }
            }

            return this.FileContent;
        }

        /// <summary>
        /// Gets the content of the s3 object.
        /// </summary>
        /// <param name="bucketName">Name of the bucket.</param>
        /// <param name="key">The key.</param>
        /// <returns>A task.</returns>
        private async Task GetS3ObjectContent(string bucketName, string key)
        {
            using (var s3 = this.ClientFactory.CreateS3Client())
            {
                using (var response = await s3.GetObjectAsync(new GetObjectRequest { BucketName = bucketName, Key = key }))
                {
                    using (var sr = new StreamReader(response.ResponseStream))
                    {
                        this.FileContent = sr.ReadToEnd();
                        this.Source = InputFileSource.S3;
                    }
                }
            }
        }
    }
}