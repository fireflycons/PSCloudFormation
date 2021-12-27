namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.State;

    /// <summary>
    /// A string scalar input variable
    /// </summary>
    /// <seealso cref="InputVariable" />
    internal class StringInputVariable : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringInputVariable"/> class.
        /// </summary>
        /// <param name="stackParameter">The AWS stack parameter to create from.</param>
        public StringInputVariable(IParameter stackParameter)
            : base(stackParameter)
        {
        }

        /// <inheritdoc />
        public override string Type => "string";

        /// <inheritdoc />
        public override string GenerateVariableAssignment()
        {
            return this.CurrentValue == null 
                       ? string.Empty 
                       : $"{this.Name} = \"{this.CurrentValue}\"";
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


            if (this.StackParameter.AllowedPattern != null)
            {
                var regex = this.StackParameter.AllowedPattern.ToString().Replace(@"\", @"\\").Replace("\"", "\\\"");
                validationExpressions.Add($"can(regex(\"{regex}\", var.{this.Name}))", false);
            }

            if (this.StackParameter.MaxLength.HasValue)
            {
                validationExpressions.Add($"length(var.{this.Name}) <= {this.StackParameter.MaxLength.Value}", true);
            }

            if (this.StackParameter.MinLength.HasValue)
            {
                validationExpressions.Add($"length(var.{this.Name}) >= {this.StackParameter.MinLength.Value}", true);
            }

            if (this.StackParameter.AllowedValues != null && this.StackParameter.AllowedValues.Any())
            {
                var items = string.Join(", ", this.StackParameter.AllowedValues.Select(v => $"\"{v}\""));
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

        /// <inheritdoc />
        protected override string GenerateDefaultStanza()
        {
            return this.DefaultValue == null ? string.Empty : $"{DefaultDeclaration}\"{this.DefaultValue}\"";
        }
    }
}