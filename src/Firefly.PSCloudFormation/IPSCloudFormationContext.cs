using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation
{
    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Utils;

    public interface IPSCloudFormationContext : ICloudFormationContext
    {
        string S3EndpointUrl { get; set; }

        string STSEndpointUrl { get; set; }

        ITimestampGenerator TimestampGenerator { get; set; }
    }
}
