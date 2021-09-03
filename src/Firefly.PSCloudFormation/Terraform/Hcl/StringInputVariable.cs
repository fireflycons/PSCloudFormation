namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Amazon.CloudFormation.Model;

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
        public StringInputVariable(ParameterDeclaration stackParameter)
            : base(stackParameter)
        {
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public override string Type => "string";

        /// <summary>
        /// Generates the default stanza.
        /// </summary>
        /// <returns>
        /// Default stanza for the variable declaration
        /// </returns>
        protected override string GenerateDefaultStanza()
        {
            var defaultValue = string.IsNullOrEmpty(this.DefaultValue) ? string.Empty : this.DefaultValue;

            return $"{DefaultDeclaration}\"{defaultValue}\"";
        }
    }
}