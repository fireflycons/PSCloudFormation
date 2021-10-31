namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    /// <summary>
    /// A block item within a resource definition
    /// Block items do not have an equals following their name.
    /// Blocks often enclosed within array in the JSON state, which must be suppressed
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.Events.HclEvent" />
    internal class BlockStart : HclEvent
    {
        /// <inheritdoc />
        public override int NestingIncrease => 1;

        /// <inheritdoc />
        internal override EventType Type => EventType.BlockStart;
    }
}