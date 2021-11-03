namespace Firefly.PSCloudFormation.Terraform.State
{
    internal class InputVariableReference : Reference
    {
        public InputVariableReference(string variableName)
            : base(variableName)
        {
        }

        public override string ReferenceExpression => $"var.{this.ObjectAddress}";
    }
}