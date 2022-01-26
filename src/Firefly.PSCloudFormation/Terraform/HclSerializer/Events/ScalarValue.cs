namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    using Firefly.PSCloudFormation.Terraform.State;

    /// <summary>
    /// A scalar value, either a mapping or sequence value.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.Events.Scalar" />
    internal class ScalarValue : Scalar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarValue"/> class.
        /// </summary>
        /// <param name="reference">A <see cref="Reference"/>, which is never quoted.</param>
        public ScalarValue(Reference reference)
            : base(reference.ReferenceExpression, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarValue"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ScalarValue(object value)
            : base(value, value is string)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this scalar contains an empty value (null, empty string or boolean false).
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty =>
            string.IsNullOrEmpty(this.Value) || (bool.TryParse(this.Value, out var boolVal) && !boolVal);

        /// <inheritdoc />
        internal override EventType Type => EventType.ScalarValue;

        public override string Repr()
        {
            if (this.Value is string s)
            {
                return $"new {this.GetType().Name}(\"{s}\")";
            }

            if (this.Value == null)
            {
                return $"new {this.GetType().Name}((string)null)";
            }

            return $"new {this.GetType().Name}({this.Value})";
        }
    }
}