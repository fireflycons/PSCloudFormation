namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;

    internal class IndirectReference : Reference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndirectReference"/> class.
        /// </summary>
        /// <param name="attributeAddress">The resource address.</param>
        public IndirectReference(string attributeAddress)
            : base(attributeAddress)
        {
        }

        /// <inheritdoc />
        public override string ReferenceExpression => $"{this.ObjectAddress}";
    }
}