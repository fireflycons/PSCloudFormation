namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Logical binary operation for comparison operators, 'and' and 'or'
    /// </summary>
    /// <seealso cref="IExpression" />
    [DebuggerDisplay("{leftExpression} {operatorName} {rightExpression}")]
    internal class BinaryOperation : IExpression
    {
        /// <summary>
        /// The expression on left of operator
        /// </summary>
        private readonly IExpression leftExpression;

        /// <summary>
        /// The operator
        /// </summary>
        private readonly MetadataToken @operator;

        /// <summary>
        /// The enumeration name of the operator (for debugger display)
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly string operatorName;

        /// <summary>
        /// The expression on right of operator
        /// </summary>
        private readonly IExpression rightExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryOperation"/> class.
        /// </summary>
        /// <param name="left">The left expression.</param>
        /// <param name="op">The operator.</param>
        /// <param name="right">The right expression.</param>
        public BinaryOperation(IExpression left, MetadataToken op, IExpression right)
        {
            this.leftExpression = left;
            this.@operator = op;
            this.rightExpression = right;
            this.operatorName = Enum.GetName(typeof(MetadataToken), op);
        }

        /// <summary>
        /// Evaluates this expression using the specified variable context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Evaluation result which will be boolean.
        /// </returns>
        /// <exception cref="ArgumentException">Unrecognized binary operator 'operator token'</exception>
        public object Evaluate(ExpressionContext context)
        {
            var left = this.leftExpression.Evaluate(context);
            var right = this.rightExpression.Evaluate(context);

            if (left != null && right != null)
            {
                Version leftAsVersion = null;
                Version rightAsVersion = null;
                var leftIsVersion = left is string l && Version.TryParse(l, out leftAsVersion);
                var rightIsVersion = right is string r && Version.TryParse(r, out rightAsVersion);

                switch (this.@operator)
                {
                    case MetadataToken.AND:

                        return (bool)left && (bool)right;

                    case MetadataToken.OR:

                        return (bool)left || (bool)right;

                    case MetadataToken.EQUALS:

                        if (leftIsVersion && rightIsVersion)
                        {
                            return leftAsVersion == rightAsVersion;
                        }

                        return (string)left == (string)right;

                    case MetadataToken.NOTEQUALS:
                    case MetadataToken.ALTNOTEQUALS:

                        if (leftIsVersion && rightIsVersion)
                        {
                            return leftAsVersion != rightAsVersion;
                        }

                        return (string)left != (string)right;

                    case MetadataToken.GREATER:

                        if (leftIsVersion && rightIsVersion)
                        {
                            return leftAsVersion > rightAsVersion;
                        }

                        return string.Compare((string)left, (string)right, StringComparison.OrdinalIgnoreCase) > 0;

                    case MetadataToken.GREATEREQUAL:

                        if (leftIsVersion && rightIsVersion)
                        {
                            return leftAsVersion >= rightAsVersion;
                        }

                        return string.Compare((string)left, (string)right, StringComparison.OrdinalIgnoreCase) >= 0;

                    case MetadataToken.LESSER:

                        if (leftIsVersion && rightIsVersion)
                        {
                            return leftAsVersion < rightAsVersion;
                        }

                        return string.Compare((string)left, (string)right, StringComparison.OrdinalIgnoreCase) < 0;

                    case MetadataToken.LESSEREQUAL:

                        if (leftIsVersion && rightIsVersion)
                        {
                            return leftAsVersion <= rightAsVersion;
                        }

                        return string.Compare((string)left, (string)right, StringComparison.OrdinalIgnoreCase) <= 0;

                    case MetadataToken.IN:

                        var rightValues = ((string)right).Split(
                            new[] { '\t', ' ' },
                            StringSplitOptions.RemoveEmptyEntries);

                        return rightValues.Any(
                            rv => string.Compare(rv, (string)left, StringComparison.OrdinalIgnoreCase) == 0);

                    default:

                        throw new ArgumentException(
                            $"Unrecognized binary operator '{Enum.GetName(typeof(MetadataToken), this.@operator)}'");
                }
            }

            return null;
        }
    }
}