namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Firefly.CloudFormationParser;

    /// <summary>
    /// Data block for AWS::AccountId pseudo-parameter
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Hcl.PseudoParameterInput" />
    // ReSharper disable once InconsistentNaming
    internal class AWSAccountIdInput : PseudoParameterInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AWSAccountIdInput"/> class.
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        public AWSAccountIdInput(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Address => "data.aws_caller_identity.current.account_id";

        /// <inheritdoc />
        public override string GenerateHcl(bool final)
        {
            return "data \"aws_caller_identity\" \"current\" {}\n";
        }
    }
}