namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class MappingEnd : HclEvent
    {
        /// <inheritdoc />
        public override int NestingIncrease => -1;

        /// <inheritdoc />
        internal override EventType Type => EventType.MappingEnd;
    }
}