namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class SequenceEnd : HclEvent
    {
        /// <inheritdoc />
        public override int NestingIncrease => -1;

        /// <inheritdoc />
        internal override EventType Type => EventType.SequenceEnd;

    }
}