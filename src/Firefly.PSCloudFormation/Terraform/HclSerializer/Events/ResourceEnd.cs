namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class ResourceEnd : HclEvent
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.ResourceEnd;

    }
}