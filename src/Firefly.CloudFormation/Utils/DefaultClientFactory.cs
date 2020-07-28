namespace Firefly.CloudFormation.Utils
{
    using Amazon.CloudFormation;
    using Amazon.Runtime;

    /// <summary>
    /// Default AWS client factory
    /// </summary>
    /// <seealso cref="IAwsClientFactory" />
    internal class DefaultClientFactory : IAwsClientFactory
    {
        /// <summary>
        /// The context
        /// </summary>
        private readonly ICloudFormationContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultClientFactory"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public DefaultClientFactory(ICloudFormationContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a CloudFormation client.
        /// </summary>
        /// <returns>
        /// A CloudFormation client.
        /// </returns>
        public IAmazonCloudFormation CreateCloudFormationClient()
        {
            var config = new AmazonCloudFormationConfig { RegionEndpoint = this.context.Region };

            if (this.context.CloudFormationEndpointUrl != null)
            {
                config.ServiceURL = this.context.CloudFormationEndpointUrl.AbsoluteUri;
            }

            return new AmazonCloudFormationClient(this.context.Credentials ?? new AnonymousAWSCredentials(), config);
        }
    }
}