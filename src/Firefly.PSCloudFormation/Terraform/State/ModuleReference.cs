namespace Firefly.PSCloudFormation.Terraform.State
{
    /// <summary>
    /// Reference to a module output, i.e. <c>module.module_name.output</c>
    /// </summary>
    internal class ModuleReference : Reference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleReference"/> class.
        /// </summary>
        /// <param name="objectAddress">The object address.</param>
        /// <param name="index">The index.</param>
        public ModuleReference(string objectAddress, int index)
            : base(objectAddress, index)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleReference"/> class.
        /// </summary>
        /// <param name="objectAddress">The object address.</param>
        public ModuleReference(string objectAddress)
            : base(objectAddress)
        {
        }

        /// <inheritdoc />
        public override string ReferenceExpression => $"module.{this.ObjectAddress}";
    }
}