namespace Firefly.CloudFormation.Utils
{
    using Amazon.CloudFormation;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.SecurityToken;

    /// <summary>
    /// Default AWS client factory
    /// </summary>
    /// <seealso cref="Firefly.CloudFormation.Utils.IAwsClientFactory" />
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
                config.ServiceURL = this.context.S3EndpointUrl.AbsoluteUri;
            }

            return new AmazonCloudFormationClient(this.context.Credentials ?? new AnonymousAWSCredentials(), config);
        }

        /// <summary>
        /// <para>
        /// Creates an s3 client.
        /// </para>
        /// <para>
        /// This is used to put templates that are larger then 51,200 bytes up to S3 prior to running the CloudFormation change. The following permissions are required
        /// <list type="bullet"><item><term>s3:CreateBucket</term><description>To create the bucket used by this library if it does not exist.</description></item><item><term>s3:PutLifecycleConfiguration</term><description>Used when creating bucket to prevent buildup of old templates.</description></item><item><term>s3:PutObject</term><description>To upload templates to the bucket.</description></item></list></para>
        /// </summary>
        /// <returns>
        /// An S3 client.
        /// </returns>
        public IAmazonS3 CreateS3Client()
        {
            var config = new AmazonS3Config { RegionEndpoint = this.context.Region };

            if (this.context.S3EndpointUrl != null)
            {
                config.ServiceURL = this.context.S3EndpointUrl.AbsoluteUri;
            }

            return new AmazonS3Client(this.context.Credentials, config);
        }
        
        /// <summary>
        /// <para>
        /// Creates an STS client.
        /// </para>
        /// <para>
        /// Principally used to determine the AWS account ID to ensure uniqueness of S3 bucket name. The following permissions are required
        /// <list type="bullet"><item><term><c>sts:GetCallerIdentity</c></term><description>To get the account ID for the caller's account.</description></item></list></para>
        /// </summary>
        /// <returns>
        /// A Security Token Service client.
        /// </returns>
        public IAmazonSecurityTokenService CreateSTSClient()
        {
            var config = new AmazonSecurityTokenServiceConfig { RegionEndpoint = this.context.Region };

            if (this.context.S3EndpointUrl != null)
            {
                config.ServiceURL = this.context.STSEndpointUrl.AbsoluteUri;
            }

            return new AmazonSecurityTokenServiceClient(this.context.Credentials, config);
        }
    }
}