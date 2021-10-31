using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal enum EventType
    {
        None,
        Scalar,
        MappingKey,
        ScalarValue,
        SequenceStart,
        SequenceEnd,
        MappingStart,
        MappingEnd,
        ResourceStart,
        ResourceEnd,
        PolicyStart,
        PolicyEnd,
        BlockStart,
        BlockEnd,
        Comment,
    }
}
