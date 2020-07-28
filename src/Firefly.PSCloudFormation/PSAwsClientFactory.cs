namespace Firefly.PSCloudFormation
{
    using Amazon.CloudFormation;
    using Amazon.S3;
    using Amazon.SecurityToken;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// <see cref="IAwsClientFactory"/> implementation for PowerShell
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class PSAwsClientFactory : IPSAwsClientFactory
    {
        /// <summary>
        /// The cloud formation client
        /// </summary>
        private readonly IAmazonCloudFormation cloudFormationClient;

        /// <summary>
        /// The context
        /// </summary>
        private readonly IPSCloudFormationContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSAwsClientFactory"/> class.
        /// </summary>
        /// <param name="cloudFormationClient">The cloud formation client.</param>
        /// <param name="context">The context.</param>
        public PSAwsClientFactory(IAmazonCloudFormation cloudFormationClient, IPSCloudFormationContext context)
        {
            this.cloudFormationClient = cloudFormationClient;
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
            return this.cloudFormationClient;
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
            if (this.context.S3EndpointUrl == null)
            {
                return new AmazonS3Client(this.context.Credentials, this.context.Region);
            }

            return new AmazonS3Client(
                this.context.Credentials,
                new AmazonS3Config
                    {
                        RegionEndpoint = this.context.Region,
                        ServiceURL = this.context.S3EndpointUrl
                    });
        }

        /// <summary>
        /// <para>
        /// Creates an STS client.
        /// </para>
        /// <para>
        /// Principally used to determine the AWS account ID to ensure uniqueness of S3 bucket name. The following permissions are required
        /// <list type="bullet"><item><term>sts:GetCallerIdentity</term><description>To get the account ID for the caller's account.</description></item></list></para>
        /// </summary>
        /// <returns>
        /// A Security Token Service client.
        /// </returns>
        // ReSharper disable once StyleCop.SA1650
        public IAmazonSecurityTokenService CreateSTSClient()
        {
            if (this.context.STSEndpointUrl == null)
            {
                return new AmazonSecurityTokenServiceClient(this.context.Credentials, this.context.Region);
            }

            return new AmazonSecurityTokenServiceClient(
                this.context.Credentials,
                new AmazonSecurityTokenServiceConfig
                    {
                        RegionEndpoint = this.context.Region,
                        ServiceURL = this.context.STSEndpointUrl
                    });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.cloudFormationClient?.Dispose();
        }
    }
}