namespace Firefly.PSCloudFormation
{
    using System;

    using Amazon;
    using Amazon.Runtime;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Utils;

    public class PSCloudFormationContext : ICloudFormationContext
    {
        public string AccountId { get; set; }

        public Uri CloudFormationEndpointUrl { get; set; }

        public AWSCredentials Credentials { get; set; }

        public ILogger Logger { get; set; }

        public RegionEndpoint Region { get; set; }

        public Uri S3EndpointUrl { get; set; }

        public Uri STSEndpointUrl { get; set; }

        public ITimestampGenerator TimestampGenerator { get; set; }
    }
}