namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Firefly.CloudFormationParser;

    /// <summary>
    /// data block for AWS::Region pseudo-parameter
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Hcl.PseudoParameterInput" />
    // ReSharper disable once InconsistentNaming
    internal class AWSRegionInput : PseudoParameterInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AWSRegionInput"/> class.
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        public AWSRegionInput(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Address => "data.aws_region.current.name";

        /// <inheritdoc />
        public override string GenerateHcl(bool final)
        {
            return "data \"aws_region\" \"current\" {}\n";
        }
    }
}