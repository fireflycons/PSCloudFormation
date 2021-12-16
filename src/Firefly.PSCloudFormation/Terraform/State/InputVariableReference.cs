namespace Firefly.PSCloudFormation.Terraform.State
{
    internal class InputVariableReference : Reference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputVariableReference"/> class.
        /// </summary>
        /// <param name="objectAddress">The object address.</param>
        public InputVariableReference(string objectAddress)
            : base(objectAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputVariableReference"/> class.
        /// </summary>
        /// <param name="objectAddress">The object address.</param>
        /// <param name="index">The index.</param>
        public InputVariableReference(string objectAddress, int index)
            : base(objectAddress, index)
        {
        }

        /// <inheritdoc />
        public override string ReferenceExpression =>
            this.Index != -1 ? $"var.{this.ObjectAddress}[{this.Index}]" : $"var.{this.ObjectAddress}";
    }
}