namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class ScalarValue : Scalar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarValue"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ScalarValue(object value)
            : base(value, value is string)
        {
        }

        /// <inheritdoc />
        internal override EventType Type => EventType.ScalarValue;
    }
}