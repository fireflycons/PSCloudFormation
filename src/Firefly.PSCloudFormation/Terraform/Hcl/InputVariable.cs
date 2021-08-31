namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Text;

    using Amazon.CloudFormation.Model;

    /// <summary>
    /// Represents a Terraform HCL parameter (input variable)
    /// </summary>
    internal abstract class InputVariable
    {
        /// <summary>
        /// Text fragment to insert when declaring input variable default
        /// </summary>
        public const string DefaultDeclaration = "  default     = ";

        /// <summary>
        /// Initializes a new instance of the <see cref="InputVariable"/> class.
        /// </summary>
        /// <param name="stackParameter">The AWS stack parameter to create from.</param>
        protected InputVariable(ParameterDeclaration stackParameter)
        {
            this.Name = stackParameter.ParameterKey;
            this.Description = stackParameter.Description;
            this.Sensitive = stackParameter.NoEcho;
            this.DefaultValue = stackParameter.DefaultValue;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="InputVariable"/> is sensitive.
        /// </summary>
        /// <value>
        ///   <c>true</c> if sensitive; otherwise, <c>false</c>.
        /// </value>
        public bool Sensitive { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public abstract string Type { get; }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        /// <value>
        /// The default value.
        /// </value>
        public string DefaultValue { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is scalar.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is scalar; otherwise, <c>false</c>.
        /// </value>
        public bool IsScalar => !this.Type.StartsWith("list(");

        /// <summary>
        /// Factory method to create an input variable of the correct type.
        /// TODO: add validation for AWS custom types. Get some commonality with cmdlet parameter parser
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        /// <returns>Subclass of <see cref="InputVariable"/></returns>
        public static InputVariable CreateParameter(ParameterDeclaration stackParameter)
        {
            switch (stackParameter.ParameterType)
            {
                case "String":
                case "AWS::EC2::AvailabilityZone::Name":
                case "AWS::EC2::Image::Id":
                case "AWS::EC2::Instance::Id":
                case "AWS::EC2::KeyPair::KeyName":
                case "AWS::EC2::SecurityGroup::GroupName":
                case "AWS::EC2::SecurityGroup::Id":
                case "AWS::EC2::Subnet::Id":
                case "AWS::EC2::Volume::Id":
                case "AWS::EC2::VPC::Id":
                case "AWS::Route53::HostedZone::Id":
                    
                    return new StringInputVariable(stackParameter);

                case "List<String>":
                case "CommaDelimitedList":
                case "List<AWS::EC2::AvailabilityZone::Name>":
                case "List<AWS::EC2::Image::Id>":
                case "List<AWS::EC2::Instance::Id>":
                case "List<AWS::EC2::SecurityGroup::GroupName>":
                case "List<AWS::EC2::SecurityGroup::Id>":
                case "List<AWS::EC2::Subnet::Id>":
                case "List<AWS::EC2::Volume::Id":
                case "List<AWS::EC2::VPC::Id>":
                case "List<AWS::Route53::HostedZone::Id>":

                    return new StringListInputVariable(stackParameter);

                case "Number":

                    return new NumericInputVariable(stackParameter);

                case "List<Number>":

                    return new NumericListInputVariable(stackParameter);
            }

            return null;
        }

        /// <summary>
        /// Generates HCL code for this parameter.
        /// </summary>
        /// <returns>HCL code for the parameter.</returns>
        public string GenerateHcl()
        {
            var hcl = new StringBuilder();

            hcl.AppendLine($"variable \"{this.Name}\" {{");
            hcl.AppendLine($"  type        = {this.Type}");

            if (!string.IsNullOrEmpty(this.Description))
            {
                hcl.AppendLine($"  description = \"{this.Description}\"");
            }

            if (this.Sensitive)
            {
                hcl.AppendLine("  sensitive = true");
            }

            hcl.AppendLine(this.GenerateDefaultStanza());
            hcl.AppendLine("}");

            return hcl.ToString();
        }

        /// <summary>
        /// Generates the default stanza.
        /// </summary>
        /// <returns>Default stanza for the variable declaration</returns>
        protected abstract string GenerateDefaultStanza();
    }
}
