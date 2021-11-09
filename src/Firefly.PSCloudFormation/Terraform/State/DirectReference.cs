namespace Firefly.PSCloudFormation.Terraform.State
{
    internal class DirectReference : Reference
    {
        public DirectReference(string resourceAddress)
            : base(resourceAddress)
        {
        }

        public override string ReferenceExpression => $"{this.ObjectAddress}.id";
    }
}