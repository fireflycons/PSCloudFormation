namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model
{
    using System.Diagnostics;

    /// <summary>
    /// Expression representing a variable.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model.IExpression" />
    [DebuggerDisplay("Variable: {variableName}")]
    internal class Variable : IExpression
    {
        /// <summary>
        /// The variable name
        /// </summary>
        private readonly string variableName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        public Variable(string varName)
        {
            this.variableName = varName;
        }

        /// <summary>
        /// Evaluates this expression using the specified variable context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Evaluation result which may be string or boolean.
        /// </returns>
        public object Evaluate(ExpressionContext context)
        {
            return context.GetValue(this.variableName);
        }
    }
}