namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class JsonStart : CollectionStart
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.JsonStart;
    }
}