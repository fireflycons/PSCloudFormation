namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    using System.Linq;

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
        public MappingKey(string key)
            : base(key, key.Any(IsPunctuation))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingKey"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="isBlockKey">if set to <c>true</c> [is block key].</param>
        public MappingKey(string key, bool isBlockKey)
        : this (key)
        {
            this.IsBlockKey = isBlockKey;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a block key.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is block key; otherwise, <c>false</c>.
        /// </value>
        public bool IsBlockKey { get; }

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
    }
}