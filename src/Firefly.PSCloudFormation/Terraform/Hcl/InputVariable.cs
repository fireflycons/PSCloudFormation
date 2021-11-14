namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Represents a Terraform HCL parameter (input variable)
    /// </summary>
    internal abstract class InputVariable : IReferencedItem
    {
        /// <summary>
        /// Text fragment to insert when declaring input variable default
        /// </summary>
        public const string DefaultDeclaration = "  default     = ";

        /// <summary>
        /// Valid pseudo parameter types
        /// </summary>
        private static readonly Dictionary<string, Type> PseudoParameterTypes = new Dictionary<string, Type>
            {
                { "AWS::Region", typeof(AWSRegionInput) },
                { "AWS::AccountId", typeof(AWSAccountIdInput) },
                { "AWS::Partition", typeof(AWSPartitionInput) },
                { "AWS::URLSuffix", typeof(AWSUrlSuffixInput) }
            };

        /// <summary>
        /// A regex that won't match anything. ARNs aren't part of the equation for parameters.
        /// </summary>
        private static readonly Regex WillNotMatchRegex = new Regex(@"[z]{50}");

        /// <summary>
        /// Initializes a new instance of the <see cref="InputVariable"/> class.
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        protected InputVariable(IParameter stackParameter)
        {
            this.StackParameter = stackParameter;
            this.CurrentValue = stackParameter.GetCurrentValue();
        }

        /// <summary>
        /// Gets the reference address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public virtual string Address => $"var.{this.Name}";

        /// <inheritdoc />
        public Regex ArnRegex => WillNotMatchRegex;

        /// <summary>
        /// Gets the current value of the parameter as returned by the stack
        /// </summary>
        /// <value>
        /// The resolved value.
        /// </value>
        public virtual object CurrentValue { get; }

        /// <summary>
        /// Gets the default value. If value is a list, then this is comma separated
        /// </summary>
        /// <value>
        /// The default value.
        /// </value>
        public string DefaultValue => this.StackParameter.Default;

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description => this.StackParameter.Description;

        /// <summary>
        /// <para>
        /// Gets a value indicating whether this instance is a data source rather than an input variable.
        /// Data sources are emitted for <c>AWS::SSM::Parameter::Value</c> and pseudo-parameters like
        /// <c>AWS::Region</c>
        /// </para>
        /// <para>
        /// If the CloudFormation parameter type is <c>AWS::SSM::Parameter::Value</c>, then in terraform
        /// this is a data source lookup, when a default (name of parameter) is supplied.
        /// </para>
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is data source; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsDataSource { get; } = false;

        /// <summary>
        /// Gets a value indicating whether this instance is scalar.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is scalar; otherwise, <c>false</c>.
        /// </value>
        public bool IsScalar => !this.Type.StartsWith("list(");

        /// <inheritdoc />
        public virtual IList<string> ListIdentity => new List<string> { this.DefaultValue };

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => this.StackParameter.Name;

        /// <inheritdoc />
        public string ScalarIdentity => this.CurrentValue.ToString();

        /// <summary>
        /// Gets a value indicating whether this <see cref="InputVariable"/> is sensitive.
        /// </summary>
        /// <value>
        ///   <c>true</c> if sensitive; otherwise, <c>false</c>.
        /// </value>
        public bool Sensitive => this.StackParameter.NoEcho;

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public abstract string Type { get; }

        /// <summary>
        /// Gets the stack parameter from the parsed template.
        /// </summary>
        protected IParameter StackParameter { get; }

        /// <summary>
        /// Factory method to create an input variable of the correct type.
        /// TODO: add validation for AWS custom types. Get some commonality with cmdlet parameter parser
        /// </summary>
        /// <param name="stackParameter">The stack parameter.</param>
        /// <returns>Subclass of <see cref="InputVariable"/></returns>
        public static InputVariable CreateParameter(IParameter stackParameter)
        {
            if (PseudoParameterTypes.ContainsKey(stackParameter.Name))
            {
                return (PseudoParameterInput)Activator.CreateInstance(PseudoParameterTypes[stackParameter.Name], stackParameter);
            }

            if (stackParameter.Name.StartsWith("AWS::"))
            {
                // Unsupported pseudo-parameter
                return null;
            }

            switch (stackParameter.Type)
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

                default:

                    if (stackParameter.Type.StartsWith("AWS::SSM::Parameter::Value")
                        && stackParameter.CurrentValue != null)
                    {
                        return new SSMParameterInput(stackParameter);
                    }

                    break;
            }

            return null;
        }

        /// <summary>
        /// Generates HCL code for this parameter.
        /// </summary>
        /// <param name="final">While resolving HCL, output the current value as the default but finally, output the true default.</param>
        /// <returns>HCL code for the parameter.</returns>
        public virtual string GenerateHcl(bool final)
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

            hcl.AppendLine(this.GenerateDefaultStanza(final));
            hcl.AppendLine(this.GenerateValidationStanza());
            hcl.AppendLine("}");

            return hcl.ToString();
        }

        /// <summary>
        /// Generates a <c>.tfvars</c> entry using the current value where it isn't the same as the default
        /// </summary>
        /// <returns>HCL variable value assignment.</returns>
        public virtual string GenerateTfVar()
        {
            var hcl = new StringBuilder();

            return hcl.ToString();
        }

        public int IndexOf(string value)
        {
            if (this.IsScalar)
            {
                return -1;
            }

            foreach (var (item, ind) in this.ListIdentity.WithIndex())
            {
                if (item == value)
                {
                    return ind;
                }
            }

            return -1;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Address;
        }

        /// <summary>
        /// Generates the default stanza.
        /// </summary>
        /// <param name="final">While resolving HCL, output the current value as the default but finally, output the true default.</param>
        /// <returns>Default stanza for the variable declaration</returns>
        protected abstract string GenerateDefaultStanza(bool final);

        /// <summary>
        /// Generates the validation stanza.
        /// </summary>
        /// <returns>Validation stanza for the variable declaration</returns>
        protected abstract string GenerateValidationStanza();
    }
}