namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class SequenceEnd : CollectionEnd
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.SequenceEnd;

    }
}