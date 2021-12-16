namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    /// <summary>
    /// Represents no event
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.Events.HclEvent" />
    internal class NullEvent : HclEvent
    {
        /// <inheritdoc />
        internal override EventType Type => EventType.None;
    }
}