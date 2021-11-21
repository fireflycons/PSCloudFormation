namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    /// <summary>
    /// Result of attribute analysis
    /// </summary>
    internal enum AttributeContent
    {
        /// <summary>
        /// No concrete analysis
        /// </summary>
        None,

        /// <summary>
        /// Next event is a scalar with value <c>null</c>
        /// </summary>
        Null,

        /// <summary>
        /// Event is empty scalar; <c>null</c>, <c>false</c> or empty string
        /// </summary>
        Empty,

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
        /// In a block set/list definition where there is a sequence of repeating blocks.
        /// </summary>
        BlockList,

        /// <summary>
        /// In a block mapping that has no embedded sequence component, like 'timeouts'
        /// </summary>
        BlockObject,

        /// <summary>
        /// Attribute introduces a sequence
        /// </summary>
        Sequence,

        /// <summary>
        /// Attribute introduces a mapping (basic object)
        /// </summary>
        Mapping,

        /// <summary>
        /// Attribute has some kind of value
        /// </summary>
        Value
    }
}