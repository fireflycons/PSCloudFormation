namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model
{
    using System.Diagnostics;

    [DebuggerDisplay("Contsant: {value}")]
    internal class Constant : IExpression
    {
        private readonly string value;

        public Constant(string value)
        {
            this.value = value;
        }

        public object Evaluate(ExpressionContext context)
        {
            return this.value;
        }
    }
}
