namespace Firefly.PSCloudFormation.Terraform.State
{
    internal class MapReference : Reference
    {
        public MapReference(string objectAddress, int index)
            : base(objectAddress, index)
        {
        }

        public MapReference(string objectAddress)
            : base(objectAddress)
        {
        }

        public override string ReferenceExpression => $"local.mappings{this.ObjectAddress}";
    }
}