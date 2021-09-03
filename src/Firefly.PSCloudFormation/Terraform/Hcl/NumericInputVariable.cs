namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Amazon.CloudFormation.Model;

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
        public NumericInputVariable(ParameterDeclaration stackParameter)
            : base(stackParameter)
        {
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public override string Type => "number";

        /// <summary>
        /// Generates the default stanza.
        /// </summary>
        /// <returns>
        /// Default stanza for the variable declaration
        /// </returns>
        protected override string GenerateDefaultStanza()
        {
            double defaultValue = 0;

            if (!string.IsNullOrEmpty(this.DefaultValue))
            {
                defaultValue = double.Parse(this.DefaultValue);
            }

            return $"{ DefaultDeclaration}{ defaultValue}";
        }
    }
}