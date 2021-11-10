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

        JsonStart,

        JsonEnd,

        BlockStart,

        BlockEnd,

        Comment
    }
}