namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class PolicyEnd : CollectionEnd
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.PolicyEnd;
    }
}