namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class PolicyStart : HclEvent
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.PolicyStart;
    }
}