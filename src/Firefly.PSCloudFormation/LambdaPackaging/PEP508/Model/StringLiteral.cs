namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model
{
    using System.Diagnostics;

    /// <summary>
    /// Expression representation of a literal string.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model.IExpression" />
    [DebuggerDisplay("Literal: {value}")]
    internal class StringLiteral : IExpression
    {
        /// <summary>
        /// The value
        /// </summary>
        private readonly string value;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringLiteral"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public StringLiteral(string value)
        {
            this.value = value;
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
            return this.value;
        }
    }
}
