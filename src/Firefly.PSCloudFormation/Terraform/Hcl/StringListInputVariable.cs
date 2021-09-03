namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Amazon.CloudFormation.Model;

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
        public StringListInputVariable(ParameterDeclaration stackParameter)
            : base(stackParameter)
        {
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public override string Type => "list(string)";

        /// <summary>
        /// Generates the default stanza.
        /// </summary>
        /// <returns>
        /// Default stanza for the variable declaration
        /// </returns>
        protected override string GenerateDefaultStanza()
        {
            var hcl = new StringBuilder();

            var defaultValue = string.IsNullOrEmpty(this.DefaultValue)
                                   ? new List<string> { string.Empty }
                                   : this.DefaultValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            hcl.AppendLine($"{DefaultDeclaration}[");
            foreach (var val in defaultValue)
            {
                hcl.AppendLine($"    \"{val}\",");
            }

            hcl.AppendLine("  ]");

            return hcl.ToString();
        }
    }
}