namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    using System.Linq;

    using Newtonsoft.Json.Linq;

    internal class Scalar : HclEvent
    {
        public Scalar(object value, bool isQuoted)
        {
            this.IsQuoted = isQuoted;

            if (value is bool)
            {
                this.Value = value.ToString().ToLowerInvariant();
            }
            else
            {
                this.Value = value.ToString();

                if (string.IsNullOrWhiteSpace(this.Value))
                {
                    return;
                }

                var firstChar = this.Value.TrimStart().First();

                if (!(firstChar == '{' || firstChar == '['))
                {
                    // Value is not JSON
                    return;
                }

                try
                {
                    // If JSON, then embedded policy document
                    var jobject = JObject.Parse(this.Value);
                    this.IsPolicyDocument = jobject.ContainsKey("Statement");
                }
                catch
                {
                    // Deliberately swallow. String is not valid JSON
                }
            }
        }

        public bool IsPolicyDocument { get; }

        public bool IsQuoted { get; }

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
                $"{this.GetType().Name}, Value = {this.Value}, IsQuoted = {this.IsQuoted}, IsPolicyDocument = {this.IsPolicyDocument}";
        }
    }
}