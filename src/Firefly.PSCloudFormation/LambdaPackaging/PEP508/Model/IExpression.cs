namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model
{
    /// <summary>
    /// Describes an expression node in the parse tree
    /// </summary>
    internal interface IExpression
    {
        /// <summary>
        /// Evaluates this expression using the specified variable context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Evaluation result which may be string or boolean.</returns>
        object Evaluate(ExpressionContext context);
    }
}