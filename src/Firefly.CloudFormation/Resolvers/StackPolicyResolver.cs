namespace Firefly.CloudFormation.Resolvers
{
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Concrete file resolver implementation for stack policy files.
    /// </summary>
    /// <seealso cref="AbstractFileResolver" />
    public class StackPolicyResolver : AbstractFileResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StackPolicyResolver"/> class.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        /// <param name="context">The context.</param>
        public StackPolicyResolver(IAwsClientFactory clientFactory, ICloudFormationContext context)
            : base(clientFactory, context)
        {
        }

        /// <summary>
        /// Gets the maximum size of the file.
        /// If the file is on local file system and is larger than this number of bytes, it must first be uploaded to S3.
        /// </summary>
        /// <value>
        /// The maximum size of the file.
        /// </value>
        protected override int MaxFileSize { get; } = 16384;
    }
}