namespace Firefly.PSCloudFormation.Terraform.State
{
    internal class ParameterReference : Reference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterReference"/> class.
        /// </summary>
        /// <param name="objectAddress">The object address.</param>
        public ParameterReference(string objectAddress)
            : base(objectAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterReference"/> class.
        /// </summary>
        /// <param name="objectAddress">The object address.</param>
        /// <param name="index">The index.</param>
        public ParameterReference(string objectAddress, int index)
            : base(objectAddress, index)
        {
        }

        /// <inheritdoc />
        public override string ReferenceExpression =>
            this.Index != -1 ? $"{this.ObjectAddress}[{this.Index}]" : this.ObjectAddress;
    }
}