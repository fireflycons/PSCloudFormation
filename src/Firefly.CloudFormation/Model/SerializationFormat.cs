namespace Firefly.CloudFormation.Model
{
    /// <summary>
    /// Data serialization format for templates, policies etc.
    /// </summary>
    public enum SerializationFormat
    {
        /// <summary>
        /// Format is JSON
        /// </summary>
        Json,

        /// <summary>
        /// Format is YAML
        /// </summary>
        Yaml,

        /// <summary>
        /// Cannot determine format. For input files being parsed, generally indicates empty or whitespace only content.
        /// </summary>
        Unknown
    }
}