namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class MappingEnd : CollectionEnd
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.MappingEnd;
    }
}