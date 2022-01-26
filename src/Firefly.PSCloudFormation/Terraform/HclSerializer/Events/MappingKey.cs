namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    /// <summary>
    /// Scalar derivative for mapping keys.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.Events.Scalar" />
    internal class MappingKey : Scalar
    {
        /// <summary>
        /// Chars that should not be treated as punctuation for emission purposes.
        /// </summary>
        private static readonly char[] NotPunctuation = { '_' };

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingKey"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="path">The path.</param>
        /// <param name="schema">The schema.</param>
        public MappingKey(string key, AttributePath path, ValueSchema schema)
            : this(key)
        {
            if (path != null)
            {
                this.Path = path;
            }

            this.Schema = schema;

            if (this.Schema.IsBlock)
            {
                this.InitialAnalysis =
                    this.Schema.IsListOrSet ? AttributeContent.BlockList : AttributeContent.BlockObject;
            }
            else if (this.Schema.IsListOrSet)
            {
                this.InitialAnalysis = AttributeContent.Sequence;
            }
            else if (this.Schema.IsScalar)
            {
                this.InitialAnalysis = AttributeContent.Value;
            }
            else if (this.Schema.Type == SchemaValueType.TypeMap)
            {
                this.InitialAnalysis = AttributeContent.Mapping;
            }
            else
            {
                this.InitialAnalysis = AttributeContent.None;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingKey"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        protected MappingKey(string key)
            : base(key, key.Any(IsPunctuation))
        {
        }

        /// <summary>
        /// Gets the initial analysis of what this key's value represents.
        /// </summary>
        /// <value>
        /// The initial analysis.
        /// </value>
        public AttributeContent InitialAnalysis { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is a block key.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is block key; otherwise, <c>false</c>.
        /// </value>
        public bool IsBlockKey => this.Schema.IsBlock;

        /// <summary>
        /// Gets the provider path for this attribute key.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; } = string.Empty;

        /// <summary>
        /// Gets the schema for the value this key represents.
        /// </summary>
        /// <value>
        /// The schema.
        /// </value>
        public ValueSchema Schema { get; }

        /// <inheritdoc />
        internal override EventType Type => EventType.MappingKey;

        /// <inheritdoc />
        public override bool Equals(HclEvent other)
        {
            if (other is null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is MappingKey mk)
            {
                return this.Path == mk.Path;
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode(HclEvent obj)
        {
            if (obj is MappingKey mk)
            {
                return mk.Path.GetHashCode();
            }

            return obj.GetHashCode();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{this.Path}, IsBlockKey = {this.IsBlockKey}";
        }

        /// <summary>
        /// Determines whether the specified character is punctuation.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>
        ///   <c>true</c> if the specified character is punctuation; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsPunctuation(char c)
        {
            return char.IsPunctuation(c) && !NotPunctuation.Contains(c);
        }
    }
}