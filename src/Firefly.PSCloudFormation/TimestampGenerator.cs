namespace Firefly.PSCloudFormation
{
    using System;

    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Concrete class to generate timestamps to form changeset names
    /// </summary>
    /// <seealso cref="Firefly.CloudFormation.Utils.ITimestampGenerator" />
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