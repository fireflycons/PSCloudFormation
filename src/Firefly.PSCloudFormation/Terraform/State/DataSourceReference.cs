namespace Firefly.PSCloudFormation.Terraform.State
{
    internal class DataSourceReference : Reference
    {
        public DataSourceReference(string blockType, string blockName, string attribute)
            : base($"{blockType}.{blockName}.{attribute}")
        {
        }

        public DataSourceReference(string objectAddress)
            : base(objectAddress)
        {
        }

        public override string ReferenceExpression => $"data.{this.ObjectAddress}";
    }
}