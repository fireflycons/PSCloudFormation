namespace Firefly.PSCloudFormation
{
    using System;

    /// <summary>
    /// Concrete class to generate timestamps to form changeset names
    /// </summary>
    /// <seealso cref="ITimestampGenerator" />
    internal class TimestampGenerator : ITimestampGenerator
    {
        /// <summary>
        /// Generates a timestamp.
        /// </summary>
        /// <returns>
        /// Timestamp as a string
        /// </returns>
        public string GenerateTimestamp()
        {
            // ReSharper disable once StringLiteralTypo
            return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        }
    }
}