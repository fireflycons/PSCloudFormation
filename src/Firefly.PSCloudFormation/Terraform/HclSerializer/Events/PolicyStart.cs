namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class PolicyStart : CollectionStart
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.PolicyStart;
    }
}