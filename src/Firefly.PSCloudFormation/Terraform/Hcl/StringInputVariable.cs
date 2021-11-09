namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Firefly.CloudFormationParser;

    /// <summary>
    /// A string scalar input variable
    /// </summary>
    /// <seealso cref="InputVariable" />
    internal class StringInputVariable : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringInputVariable"/> class.
        /// </summary>
        /// <param name="stackParameter">The AWS stack parameter to create from.</param>
        public StringInputVariable(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Type => "string";

        /// <inheritdoc />
        protected override string GenerateDefaultStanza(bool final)
        {
            if (final)
            {
                if (this.DefaultValue == null)
                {
                    return string.Empty;
                }

                return $"{DefaultDeclaration}\"{this.DefaultValue}\"";
            }

            return this.CurrentValue == null ? string.Empty : $"{DefaultDeclaration}\"{this.CurrentValue}\"";
        }
    }
}