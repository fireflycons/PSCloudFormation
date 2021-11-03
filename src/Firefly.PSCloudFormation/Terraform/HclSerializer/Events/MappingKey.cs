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