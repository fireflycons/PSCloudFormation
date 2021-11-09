namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    /// <summary>
    /// A string list input variable
    /// </summary>
    /// <seealso cref="InputVariable" />
    internal class StringListInputVariable : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringListInputVariable"/> class.
        /// </summary>
        /// <param name="stackParameter">The AWS stack parameter to create from.</param>
        public StringListInputVariable(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Type => "list(string)";

        /// <inheritdoc />
        public override IList<string> ListIdentity => this.CurrentValueToList();

        /// <inheritdoc />
        protected override string GenerateDefaultStanza(bool final)
        {
            var hcl = new StringBuilder();
            List<string> @default;

            if (final)
            {
                @default = string.IsNullOrEmpty(this.DefaultValue)
                               ? new List<string> { string.Empty }
                               : this.DefaultValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                @default = this.CurrentValueToList();

                if (@default == null)
                {
                    return string.Empty;
                }
            }

            hcl.AppendLine($"{DefaultDeclaration}[");
            foreach (var val in @default)
            {
                hcl.AppendLine($"    \"{val}\",");
            }

            hcl.Append("  ]");

            return hcl.ToString();
        }

        /// <inheritdoc />
        protected override string GenerateValidationStanza()
        {
            return string.Empty;
        }

        private List<string> CurrentValueToList()
        {
            List<string> strings;

            switch (this.CurrentValue)
            {
                case null:

                    return null;

                case string s:

                    strings = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(str => str.Trim()).ToList();
                    break;

                case IEnumerable enumerable:

                    strings = new List<string>();

                    foreach (var val in enumerable)
                    {
                        strings.Add(val.ToString().Trim());
                    }

                    break;

                default:
                    throw new HclSerializerException(
                        $"Cannot serialize input variable of type {this.CurrentValue.GetType().Name}");

            }

            return strings;
        }
    }
}