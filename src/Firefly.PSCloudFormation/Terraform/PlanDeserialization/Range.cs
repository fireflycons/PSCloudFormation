namespace Firefly.PSCloudFormation.Terraform.PlanDeserialization
{
    using Newtonsoft.Json;

    /// <summary>
    /// Describes range within input file where the error was found
    /// </summary>
    internal class Range
    {
        /// <summary>
        /// Gets the end location.
        /// </summary>
        /// <value>
        /// The end.
        /// </value>
        [JsonProperty("end")]
        public Location End { get; private set; }

        /// <summary>
        /// Gets the filename for the file containing the error.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        [JsonProperty("filename")]
        public string Filename { get; private set; }

        /// <summary>
        /// Gets the start location.
        /// </summary>
        /// <value>
        /// The start.
        /// </value>
        [JsonProperty("start")]
        public Location Start { get; private set; }
    }
}