namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Firefly.PSCloudFormation.Terraform.State;

    internal class ModuleInputVariable : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleInputVariable"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public ModuleInputVariable(string name, Reference value)
            : base(new DummyParameter(name))
        {
            this.CurrentValue = value;
        }

        /// <inheritdoc />
        public override object CurrentValue { get; }

        /// <inheritdoc />
        public override string Type => "string";

        /// <inheritdoc />
        protected override string GenerateDefaultStanza()
        {
            return string.Empty;
        }

        /// <inheritdoc />
        protected override string GenerateValidationStanza()
        {
            return string.Empty;
        }

        /// <inheritdoc />
        public override string GenerateVariableAssignment()
        {
            return $"{this.Name} = {((Reference)this.CurrentValue).ReferenceExpression}";
        }
    }
}