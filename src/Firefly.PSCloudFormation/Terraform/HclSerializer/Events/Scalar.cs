namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;
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

            switch (value)
            {
                case null:
                    return;

                // ReSharper disable once AssignNullToNotNullAttribute - typeof an existing type cannot be null
                case string s when s.StartsWith(typeof(Reference).Namespace):
                    {
                        // Re-hydrate a smuggled in "Reference" type.
                        var split = s.Split(':');
                        var type = Assembly.GetCallingAssembly().GetType(split[0]);
                        var reference = (Reference)Activator.CreateInstance(type, split[1]);
                        this.Value = reference.ReferenceExpression;
                        this.IsQuoted = false;
                        return;
                    }

                case bool _:

                    this.Value = value.ToString().ToLowerInvariant();
                    this.IsQuoted = false;
                    break;

                default:
                    {
                        this.Value = value.ToString();

                        if (string.IsNullOrWhiteSpace(this.Value))
                        {
                            return;
                        }

                        this.IsJsonDocument = StateFileSerializer.TryGetJson(
                            this.Value,
                            false,
                            "Unknown",
                            "Unknown",
                            out var document);

                        if (this.IsJsonDocument)
                        {
                            this.JsonDocumentType = document.Type;
                        }

                        break;
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
        /// Gets a value indicating whether the value should be quoted when emitted.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is quoted; otherwise, <c>false</c>.
        /// </value>
        public bool IsQuoted { get; }

        /// <summary>
        /// Gets the type of the JSON document if <see cref="IsJsonDocument"/> is <c>true</c>.
        /// </summary>
        /// <value>
        /// The type of the JSON document.
        /// </value>
        public JTokenType JsonDocumentType { get; } = JTokenType.None;

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; }

        /// <inheritdoc />
        internal override EventType Type => EventType.Scalar;

        /// <inheritdoc />
        public override bool Equals(HclEvent other)
        {
            if (base.Equals(other) && other is Scalar scalar)
            {
                return this.Value == scalar.Value;
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode(HclEvent obj)
        {
            if (obj is Scalar scalar && scalar.Value != null)
            {
                return scalar.Value.GetHashCode();
            }

            // Null valued scalars are equivalent by virtue of being the same type.
            return obj.Type.GetHashCode();
        }

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

        /// <summary>
        /// Analyzes the value in this scalar based on it's key schema and any additional resource traits.
        /// </summary>
        /// <param name="key">The attribute key associated with this value.</param>
        /// <param name="resourceTraits">The resource traits.</param>
        /// <returns>Value analysis.</returns>
        /// <exception cref="System.InvalidOperationException">Invalid \"{key.Schema.Type}\" for scalar value at \"{key.Path}\"</exception>
        public AttributeContent Analyze(MappingKey key, IResourceTraits resourceTraits)
        {
            var value = this.Value;

            if (!key.Schema.Optional)
            {
                return AttributeContent.Value;
            }

            if (resourceTraits.IsOmittedConditionalAttrbute(key.Path, value))
            {
                return AttributeContent.Empty;
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (key.Schema.Type)
            {
                case SchemaValueType.TypeBool:

                    if (bool.TryParse(value, out var boolValue) && boolValue)
                    {
                        return AttributeContent.Value;
                    }

                    break;

                case SchemaValueType.TypeInt:

                    if (int.TryParse(value, out var intValue) && intValue != 0)
                    {
                        return AttributeContent.Value;
                    }

                    break;

                case SchemaValueType.TypeFloat:

                    if (double.TryParse(value, out var doubleValue) && doubleValue != 0)
                    {
                        return AttributeContent.Value;
                    }

                    break;

                case SchemaValueType.TypeString:

                    return string.IsNullOrEmpty(value) ? AttributeContent.Empty : AttributeContent.Value;

                case SchemaValueType.TypeJsonData:

                    // Always emit
                    return AttributeContent.Value;

                case SchemaValueType.TypeSet:
                case SchemaValueType.TypeList:
                case SchemaValueType.TypeObject:
                case SchemaValueType.TypeMap:

                    // If these are empty, then they can be represented by a null scalar
                    if (string.IsNullOrEmpty(value))
                    {
                        return AttributeContent.Empty;
                    }

                    throw new InvalidOperationException($"Unexpected scalar value for attribute of \"{key.Schema.Type}\" at \"{key.Path}\"");

                default:

                    throw new InvalidOperationException($"Invalid SchemaValueType:\"{key.Schema.Type}\" for scalar value of attribute \"{key.Path}\".");
            }

            if (!string.IsNullOrEmpty(value) && char.IsLetter(value.First()) && value.Contains("."))
            {
                // A reference
                return AttributeContent.Value;
            }

            return AttributeContent.Empty;
        }


        public override string Repr()
        {
            return $"new {this.GetType().Name}(\"{this.Value}\", {this.IsQuoted.ToString().ToLowerInvariant()})";
        }
    }
}