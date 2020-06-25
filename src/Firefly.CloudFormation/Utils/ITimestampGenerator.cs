namespace Firefly.CloudFormation.Utils
{
    /// <summary>
    /// Interface that defines a timestamp generator to form timestamp part of a changeset name.
    /// Unit tests would implement this to provide a predictable value when testing changesets.
    /// </summary>
    public interface ITimestampGenerator
    {
        /// <summary>
        /// Generates a timestamp.
        /// </summary>
        /// <returns>Timestamp as a string</returns>
        string GenerateTimestamp();
    }
}