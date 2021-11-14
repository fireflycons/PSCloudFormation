namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Firefly.CloudFormationParser;


    // ReSharper disable once InconsistentNaming
    internal class AWSUrlSuffixInput : PseudoParameterInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AWSUrlSuffixInput"/> class.
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        public AWSUrlSuffixInput(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Address => "data.aws_partition.url_suffix.dns_suffix";

        /// <inheritdoc />
        public override string GenerateHcl(bool final)
        {
            return "data \"aws_partition\" \"url_suffix\" {}\n";
        }
    }
}