namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using Firefly.CloudFormationParser;

    /// <summary>
    /// Data block for AWS::Partition pseudo-parameter
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Hcl.PseudoParameterInput" />

    // ReSharper disable once InconsistentNaming
    internal class AWSPartitionInput : PseudoParameterInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AWSPartitionInput"/> class.
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        public AWSPartitionInput(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Address => "data.aws_partition.partition.partition";

        /// <inheritdoc />
        public override string GenerateHcl(bool final)
        {
            return "data \"aws_partition\" \"partition\" {}\n";
        }
    }
}