namespace Firefly.PSCloudFormation.Terraform.PlanDeserialization
{
    using Newtonsoft.Json;

    /// <summary>
    /// Describes a location within the HCL script file
    /// </summary>
    internal class Location
    {
        /// <summary>
        /// Gets the byte number within the snippet block.
        /// </summary>
        /// <value>
        /// The byte.
        /// </value>
        [JsonProperty("@byte")]
        public int Byte { get; private set; }

        /// <summary>
        /// Gets the column.
        /// </summary>
        /// <value>
        /// The column.
        /// </value>
        [JsonProperty("column")]
        public int Column { get; private set; }

        /// <summary>
        /// Gets the line.
        /// </summary>
        /// <value>
        /// The line.
        /// </value>
        [JsonProperty("line")]
        public int Line { get; private set; }
    }
}