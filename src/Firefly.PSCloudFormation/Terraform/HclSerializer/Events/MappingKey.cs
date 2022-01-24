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
        protected MappingKey(string key)
            : base(key, key.Any(IsPunctuation))
        {
        }

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

        public ValueSchema Schema { get; }

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

        public string Path { get; } = string.Empty;

        /// <inheritdoc />
        internal override EventType Type => EventType.MappingKey;

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

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{base.ToString()}, IsBlockKey = {this.IsBlockKey}";
        }


        public bool ShouldEmitAttribute(AttributeContent analysis)
        {
            if (this.Schema.Required)
            {
                return true;
            }

            if (this.Schema.Computed && !this.Schema.Optional)
            {
                return false;
            }

            return new[]
                       {
                           AttributeContent.BlockList, AttributeContent.BlockObject, AttributeContent.Sequence,
                           AttributeContent.Mapping, AttributeContent.Value
                       }.Contains(analysis);
        }

    }
}