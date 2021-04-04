namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This object holds variables and their values to use when evaluating the parse tree.
    /// </summary>
    internal class ExpressionContext
    {
        /// <summary>
        /// The variables
        /// </summary>
        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionContext"/> class.
        /// </summary>
        /// <param name="variables">The variables.</param>
        public ExpressionContext(Dictionary<string, string> variables)
        {
            this.variables = variables;
        }

        /// <summary>
        /// Gets the value of the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The variable's value</returns>
        /// <exception cref="ArgumentException">Variable not found: {variable}</exception>
        public object GetValue(string variable)
        {
            if (!this.variables.ContainsKey(variable))
            {
                throw new ArgumentException($"Variable not found: {variable}");
            }

            return this.variables[variable];
        }
    }
}