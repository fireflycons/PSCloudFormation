namespace Firefly.CloudFormation.CloudFormation
{
    /// <summary>
    /// Format of an input file
    /// </summary>
    public enum InputFileFormat
    {
        /// <summary>
        /// File format JSON
        /// </summary>
        Json,

        /// <summary>
        /// File format YAML
        /// </summary>
        Yaml,

        /// <summary>
        /// File contains no data
        /// </summary>
        Empty
    }
}