namespace Firefly.PSCloudFormation.Terraform.State
{
    /// <summary>
    /// A normal <c>!GetAtt</c> reference to a named property within a resource.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.State.Reference" />
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