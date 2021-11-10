namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Firefly.CloudFormationParser;

    /// <summary>
    /// Used to generate data sources for AWS::Region and AWS::AccountId pseudo parameters
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Hcl.InputVariable" />
    internal abstract class PseudoParameterInput : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PseudoParameterInput"/> class.
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        protected PseudoParameterInput(IParameter stackParameter)
            : base(stackParameter)
        {
        }
        
        /// <inheritdoc />
        public override bool IsDataSource => true;

        /// <inheritdoc />
        public override string Type => "string";

        /// <inheritdoc />
        protected override string GenerateDefaultStanza(bool final)
        {
            return string.Empty;
        }

        /// <inheritdoc />
        protected override string GenerateValidationStanza()
        {
            return string.Empty;
        }
    }
}
