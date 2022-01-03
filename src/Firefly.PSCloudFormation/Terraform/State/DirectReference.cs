namespace Firefly.PSCloudFormation.Terraform.State
{
    /// <summary>
    /// Direct reference (i.e. <c>!Ref</c>) to a resource
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.State.Reference" />
    internal class DirectReference : Reference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectReference"/> class.
        /// </summary>
        /// <param name="resourceAddress">The resource address.</param>
        public DirectReference(string resourceAddress)
            : base(resourceAddress)
        {
        }

        /// <inheritdoc />
        public override string ReferenceExpression => $"{this.ObjectAddress}.id";
    }
}