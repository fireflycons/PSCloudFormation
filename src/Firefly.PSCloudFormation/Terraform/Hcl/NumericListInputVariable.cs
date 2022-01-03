namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    /// <summary>
    /// A numeric list input variable
    /// </summary>
    /// <seealso cref="InputVariable" />
    internal class NumericListInputVariable : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericListInputVariable"/> class.
        /// </summary>
        /// <param name="stackParameter">The AWS stack parameter to create from.</param>
        public NumericListInputVariable(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Type => "list(number)";

        /// <inheritdoc />
        public override IList<string> ListIdentity => this.CurrentValueToList().Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();

        /// <inheritdoc />
        public override string GenerateVariableAssignment()
        {
            return this.CurrentValue == null 
                       ? string.Empty 
                       : new StringBuilder().AppendLine($"{this.Name} = [")
                               .AppendLine(string.Join(",\n", this.ListIdentity.Select(v => $"  {v}"))).AppendLine("]")
                               .ToString();
        }

        /// <inheritdoc />
        protected override string GenerateDefaultStanza()
        {
            var hcl = new StringBuilder();

            var @default = string.IsNullOrEmpty(this.DefaultValue)
                               ? new List<double> { 0 }
                               : this.DefaultValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(double.Parse).ToList();

            hcl.AppendLine($"{DefaultDeclaration}[");
            foreach (var val in @default)
            {
                hcl.AppendLine($"    {val},");
            }

            hcl.AppendLine("  ]");

            return hcl.ToString();
        }

        /// <inheritdoc />
        protected override string GenerateValidationStanza()
        {
            return string.Empty;
        }

        /// <summary>
        /// Converts the current value to a list.
        /// </summary>
        /// <returns>A list of numeric values</returns>
        /// <exception cref="Firefly.PSCloudFormation.Terraform.HclSerializer.HclSerializerException">null - null - Cannot serialize input variable of type {this.CurrentValue.GetType().Name}</exception>
        private IEnumerable<double> CurrentValueToList()
        {
            List<double> doubles;

            switch (this.CurrentValue)
            {
                case null:

                    return null;

                case string s:

                    doubles = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(str => double.Parse(str.Trim())).ToList();
                    break;

                case IEnumerable enumerable:

                    doubles = (from object val in enumerable select double.Parse(val.ToString())).ToList();

                    break;

                default:
                    throw new HclSerializerException(null, null, $"Cannot serialize input variable of type {this.CurrentValue.GetType().Name}");

            }

            return doubles;
        }
    }
}