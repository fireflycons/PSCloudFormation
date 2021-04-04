namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model
{
    using System;

    /// <summary>
    /// Logical unary operation - NOT
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model.IExpression" />
    internal class UnaryOperation : IExpression
    {
        /// <summary>
        /// The operator
        /// </summary>
        private readonly MetadataToken @operator;

        /// <summary>
        /// The expression to the right of the operator.
        /// </summary>
        private readonly IExpression rightExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryOperation"/> class.
        /// </summary>
        /// <param name="op">The op.</param>
        /// <param name="right">The right.</param>
        public UnaryOperation(MetadataToken op, IExpression right)
        {
            this.@operator = op;
            this.rightExpression = right;
        }

        /// <summary>
        /// Evaluates this expression using the specified variable context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Evaluation result which may be string or boolean.
        /// </returns>
        /// <exception cref="ArgumentException">Unrecognized unary operator '{Enum.GetName(typeof(MetadataToken), this.@operator)}'</exception>
        public object Evaluate(ExpressionContext context)
        {
            var right = this.rightExpression.Evaluate(context);

            switch (this.@operator)
            {
                case MetadataToken.NOT:

                    return !(bool)right;

                default:

                    throw new ArgumentException($"Unrecognized unary operator '{Enum.GetName(typeof(MetadataToken), this.@operator)}'");
            }
        }
    }
}