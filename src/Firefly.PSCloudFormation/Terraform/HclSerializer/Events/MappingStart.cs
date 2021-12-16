namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class MappingStart : CollectionStart
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.MappingStart;

    }
}