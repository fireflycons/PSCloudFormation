namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class SequenceStart : CollectionStart
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.SequenceStart;

    }
}