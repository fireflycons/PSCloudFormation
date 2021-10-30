namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class PolicyEnd : HclEvent
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.PolicyEnd;
    }
}