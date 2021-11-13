namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    /// <summary>
    /// Result of attribute analysis
    /// </summary>
    internal enum AttributeContent
    {
        /// <summary>
        /// Next event is a scalar with value <c>null</c>
        /// </summary>
        Null,

        /// <summary>
        /// Next event is a scalar with value empty string
        /// </summary>
        EmptyString,

        /// <summary>
        /// Next event is a scalar with value <c>false</c>
        /// </summary>
        BooleanFalse,

        /// <summary>
        /// Next group of events is an empty sequence, mapping, block including any nesting of the same.
        /// </summary>
        EmptyCollection,

        /// <summary>
        /// Attribute has some kind of value
        /// </summary>
        HasValue
    }
}