namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class BlockEnd : HclEvent
    {
        /// <inheritdoc />
        public override int NestingIncrease => -1;

        /// <inheritdoc />
        internal override EventType Type => EventType.BlockEnd;
    }
}