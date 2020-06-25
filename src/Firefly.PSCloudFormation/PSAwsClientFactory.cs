namespace Firefly.PSCloudFormation
{
    using System;

    using Amazon.CloudFormation;
    using Amazon.S3;
    using Amazon.SecurityToken;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Utils;

    public class PSAwsClientFactory : IAwsClientFactory, IDisposable
    {
        private readonly IAmazonCloudFormation cloudFormationClient;

        private readonly ICloudFormationContext context;

        public PSAwsClientFactory(IAmazonCloudFormation cloudFormationClient, ICloudFormationContext context)
        {
            this.cloudFormationClient = cloudFormationClient;
            this.context = context;
        }

        public IAmazonCloudFormation CreateCloudFormationClient()
        {
            return this.cloudFormationClient;
        }

        public IAmazonS3 CreateS3Client()
        {
            return new AmazonS3Client(
                this.context.Credentials,
                new AmazonS3Config
                    {
                        RegionEndpoint = this.context.Region,
                        ServiceURL = this.context.S3EndpointUrl == null ? null : this.context.S3EndpointUrl.AbsoluteUri
                    });
        }

        public IAmazonSecurityTokenService CreateSTSClient()
        {
            return new AmazonSecurityTokenServiceClient(
                this.context.Credentials,
                new AmazonSecurityTokenServiceConfig
                    {
                        RegionEndpoint = this.context.Region,
                        ServiceURL = this.context.STSEndpointUrl == null
                                         ? null
                                         : this.context.STSEndpointUrl.AbsoluteUri
                    });
        }

        public void Dispose()
        {
            this.cloudFormationClient?.Dispose();
        }
    }
}