namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model
{
    using System.Diagnostics;

    /// <summary>
    /// Represents a grouped expression in parentheses.
    /// </summary>
    /// <seealso cref="IExpression" />
    [DebuggerDisplay("( {innerExpression} )")]
    internal class Group : IExpression
    {
        /// <summary>
        /// The inner expression
        /// </summary>
        private readonly IExpression innerExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="Group"/> class.
        /// </summary>
        /// <param name="expr">The expression within the group.</param>
        public Group(IExpression expr)
        {
            this.innerExpression = expr;
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
            return this.innerExpression.Evaluate(context);
        }
    }
}