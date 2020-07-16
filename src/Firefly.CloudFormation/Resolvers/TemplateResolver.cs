namespace Firefly.CloudFormation.Resolvers
{
    using System.Threading.Tasks;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Concrete file resolver implementation for CloudFormation template.
    /// </summary>
    /// <seealso cref="AbstractFileResolver" />
    public class TemplateResolver : AbstractFileResolver
    {
        /// <summary>
        /// The stack name
        /// </summary>
        private readonly string stackName;

        /// <summary>
        /// If <c>true</c> then update operations should reuse the existing template that is associated with the stack that you are updating
        /// </summary>
        private readonly bool usePreviousTemplate;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateResolver"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="stackName">Name of the stack.</param>
        /// <param name="usePreviousTemplate">if set to <c>true</c> reuse the existing template that is associated with the stack that you are updating.</param>
        public TemplateResolver(ICloudFormationContext context, string stackName, bool usePreviousTemplate)
            : this(new DefaultClientFactory(context), context, stackName, usePreviousTemplate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateResolver"/> class.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        /// <param name="context">The context.</param>
        /// <param name="stackName">Name of the stack.</param>
        /// <param name="usePreviousTemplate">if set to <c>true</c> [use previous template].</param>
        public TemplateResolver(IAwsClientFactory clientFactory, ICloudFormationContext context, string stackName, bool usePreviousTemplate)
            : base(clientFactory, context)
        {
            this.usePreviousTemplate = usePreviousTemplate;
            this.stackName = stackName;
        }

        /// <summary>
        /// Gets the maximum size of the file.
        /// If the file is on local file system and is larger than this number of bytes, it must first be uploaded to S3.
        /// </summary>
        /// <value>
        /// The maximum size of the file.
        /// </value>
        protected override int MaxFileSize { get; } = 51200;

        /// <summary>
        /// Resolves and loads the given file from the specified location
        /// </summary>
        /// <param name="objectLocation">The file location.</param>
        /// <returns>
        /// The file content
        /// </returns>
        public override async Task<string> ResolveFileAsync(string objectLocation)
        {
            if (this.usePreviousTemplate)
            {
                using (var cfn = this.ClientFactory.CreateCloudFormationClient())
                {
                    this.FileContent = (await cfn.GetTemplateAsync(new GetTemplateRequest { StackName = this.stackName }))
                        .TemplateBody;
                    this.Source = InputFileSource.UsePreviousTemplate;
                }
            }
            else
            {
                await base.ResolveFileAsync(objectLocation);
            }

            return this.FileContent;
        }
    }
}