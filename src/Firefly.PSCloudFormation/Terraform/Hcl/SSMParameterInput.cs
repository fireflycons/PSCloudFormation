namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Text;

    using Firefly.CloudFormationParser;

    /// <summary>
    /// Not strictly an input variable.
    /// When a CloudFormation parameter refers to SSM, then this must be rendered
    /// as a data source in terraform.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Hcl.InputVariable" />
    // ReSharper disable once InconsistentNaming
    internal class SSMParameterInput : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SSMParameterInput"/> class.
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        public SSMParameterInput(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        public override bool IsDataSource => true;

        /// <inheritdoc />
        public override string Type => "string";

        /// <inheritdoc />
        public override string Address => $"data.aws_ssm_parameter.{this.Name}.value";

        /// <inheritdoc />
        public override string GenerateHcl(bool final)
        {
            var hcl = new StringBuilder();

            hcl.AppendLine($"data \"aws_ssm_parameter\" \"{this.Name}\" {{");
            hcl.AppendLine($"  name = \"{this.DefaultValue}\"");
            hcl.AppendLine("}");

            return hcl.ToString();
        }

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
    }
}