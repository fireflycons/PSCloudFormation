namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    using System;
    using System.Reflection;

    using Firefly.PSCloudFormation.Terraform.State;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Base class for scalar types.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.Events.HclEvent" />
    internal class Scalar : HclEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Scalar"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="isQuoted">if set to <c>true</c> [is quoted].</param>
        public Scalar(object value, bool isQuoted)
        {
            this.IsQuoted = isQuoted;

            if (value == null)
            {
                return;
            }

            if (value is string s && s.StartsWith(typeof(Reference).Namespace))
            {
                // Re-hydrate a smuggled in "Reference" type.
                var split = s.Split(':');
                var type = Assembly.GetCallingAssembly().GetType(split[0]);
                var reference = (Reference)Activator.CreateInstance(type, split[1]);
                this.Value = reference.ReferenceExpression;
                this.IsQuoted = false;
                return;
            }

            if (value is bool)
            {
                this.Value = value.ToString().ToLowerInvariant();
                this.IsQuoted = false;
            }
            else
            {
                this.Value = value.ToString();

                if (string.IsNullOrWhiteSpace(this.Value))
                {
                    return;
                }

                this.IsJsonDocument = StateFileSerializer.TryGetJson(this.Value, false, "Unknown", "Unknown", out var document);

                if (this.IsJsonDocument)
                {
                    this.JsonDocumentType = document.Type;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the value contains a policy document
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is policy document; otherwise, <c>false</c>.
        /// </value>
        public bool IsJsonDocument { get; }

        /// <summary>
        /// Gets the type of the JSON document if <see cref="IsJsonDocument"/> is <c>true</c>.
        /// </summary>
        /// <value>
        /// The type of the JSON document.
        /// </value>
        public JTokenType JsonDocumentType { get; } = JTokenType.None;
             
        /// <summary>
        /// Gets a value indicating whether the value should be quoted when emitted.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is quoted; otherwise, <c>false</c>.
        /// </value>
        public bool IsQuoted { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; }

        /// <inheritdoc />
        internal override EventType Type => EventType.Scalar;

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return
                $"{this.GetType().Name}, Value = {(this.Value == null ? "<null>" : this.Value == string.Empty ? "<empty>" : this.Value)}, IsQuoted = {this.IsQuoted}, IsJsonDocument = {this.IsJsonDocument}";
        }
    }
}