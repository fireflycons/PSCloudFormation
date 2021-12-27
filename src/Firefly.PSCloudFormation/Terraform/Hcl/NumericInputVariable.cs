namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.State;

    /// <summary>
    /// A numeric scalar input variable
    /// </summary>
    /// <seealso cref="InputVariable" />
    internal class NumericInputVariable : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericInputVariable"/> class.
        /// </summary>
        /// <param name="stackParameter">The AWS stack parameter to create from.</param>
        public NumericInputVariable(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Type => "number";

        /// <inheritdoc />
        public override string GenerateVariableAssignment()
        {
            return this.CurrentValue == null 
                       ? string.Empty
                       : $"{this.Name} = {this.CurrentValue}";
        }

        /// <inheritdoc />
        protected override string GenerateDefaultStanza()
        {
            return this.CurrentValue != null
                       ? $"{DefaultDeclaration}{double.Parse(this.CurrentValue.ToString())}"
                       : string.Empty;
        }

        /// <inheritdoc />
        protected override string GenerateValidationStanza()
        {
            var hcl = new StringBuilder();
            var validationExpressions = new Dictionary<string, bool>();
            var errorMessage = this.StackParameter.ConstraintDescription != null
                                   ? this.StackParameter.ConstraintDescription.Replace("\"", "\\\"")
                                   : "Value does not match provided validation constraint.";

            // Fussy terraform
            if (!char.IsUpper(errorMessage.First()))
            {
                errorMessage = errorMessage[0].ToString().ToUpperInvariant() + errorMessage.Substring(1);
            }

            if (!errorMessage.EndsWith("."))
            {
                errorMessage += ".";
            }


            if (this.StackParameter.MaxValue.HasValue)
            {
                validationExpressions.Add($"var.{this.Name} <= {this.StackParameter.MaxValue.Value}", true);
            }

            if (this.StackParameter.MinValue.HasValue)
            {
                validationExpressions.Add($"var.{this.Name} >= {this.StackParameter.MinValue.Value}", true);
            }

            if (this.StackParameter.AllowedValues != null && this.StackParameter.AllowedValues.Any())
            {
                var items = string.Join(", ", this.StackParameter.AllowedValues);
                validationExpressions.Add($"contains([{items}], var.{this.Name})", false);
            }

            if (!validationExpressions.Any())
            {
                return string.Empty;
            }

            var conditions = string.Join(" && ", validationExpressions.Select(e => e.Value ? $"({e.Key})" : e.Key));

            hcl.AppendLine("  validation {");
            hcl.AppendLine($"    condition     = {conditions}");
            hcl.AppendLine(
                $"    error_message = \"{errorMessage}\"");
            hcl.Append("  }");

            return hcl.ToString();
        }
    }
}