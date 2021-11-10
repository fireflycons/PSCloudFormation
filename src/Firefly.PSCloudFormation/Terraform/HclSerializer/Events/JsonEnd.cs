namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class JsonEnd : CollectionEnd
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.JsonEnd;
    }
}