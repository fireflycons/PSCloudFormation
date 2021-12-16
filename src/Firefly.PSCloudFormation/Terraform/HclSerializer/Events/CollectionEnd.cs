namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal abstract class CollectionEnd : HclEvent
    {
        /// <inheritdoc />
        public override int NestingIncrease => -1;
    }
}