namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Firefly.CloudFormationParser;

    /// <summary>
    /// A numeric scalar input variable
    /// </summary>
    /// <seealso cref="InputVariable" />
    internal class NumericInputVariable : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericInputVariable"/> class.
        /// </summary>
        /// <param name="stackParameter">The AWS stack parameter to create from.</param>
        public NumericInputVariable(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Type => "number";

        /// <inheritdoc />
        protected override string GenerateDefaultStanza(bool final)
        {
            if (final)
            {
                if (!string.IsNullOrEmpty(this.DefaultValue))
                {
                    return $"{DefaultDeclaration}{double.Parse(this.DefaultValue)}";
                }
            }
            else
            {
                if (this.CurrentValue != null)
                {
                    return $"{DefaultDeclaration}{double.Parse(this.CurrentValue.ToString())}";
                }
            }

            return string.Empty;
        }
    }
}