namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class MappingKey : Scalar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingKey"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public MappingKey(string key)
            : base(key, false)
        {
        }

        /// <inheritdoc />
        internal override EventType Type => EventType.MappingKey;
    }
}