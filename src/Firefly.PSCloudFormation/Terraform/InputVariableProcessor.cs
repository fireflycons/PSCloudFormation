namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormation;
    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.PSCloudFormation.Terraform.Hcl;

    /// <summary>
    /// Manages conversion of CloudFormation Stack parameters to Terraform input variables.
    /// </summary>
    internal class InputVariableProcessor
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The template
        /// </summary>
        private readonly ITemplate template;

        /// <summary>
        /// The warnings
        /// </summary>
        private readonly IList<string> warnings;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputVariableProcessor"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="warnings">The warnings.</param>
        public InputVariableProcessor(ITerraformExportSettings settings, IList<string> warnings)
        {
            this.warnings = warnings;
            this.logger = settings.Logger;
            this.template = settings.Template;
        }

        /// <summary>
        /// Processes the inputs.
        /// </summary>
        /// <returns>List of <see cref="InputVariable"/></returns>
        public List<InputVariable> ProcessInputs()
        {
            this.logger.LogInformation("- Importing parameters...");
            var inputVariables = new List<InputVariable>();

            foreach (var p in this.template.Parameters.Concat(this.template.PseudoParameters))
            {
                var inputVariable = InputVariable.CreateParameter(p);

                if (inputVariable == null)
                {
                    var wrn = p is PseudoParameter
                                  ? $"Pseudo-parameter '{p.Name}' cannot be imported as it is not supported by Terraform."
                                  : $"Stack parameter '{p.Name}' cannot be imported.";

                    this.logger.LogWarning(wrn);

                    if (!this.warnings.Contains(wrn))
                    {
                        // When importing multiple stacks, only warn about unsupported pseudo vars once.
                        this.warnings.Add(wrn);
                    }
                }
                else
                {
                    inputVariables.Add(inputVariable);
                }
            }

            return inputVariables;
        }
    }
}