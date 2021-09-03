namespace Firefly.PSCloudFormation.Terraform.PlanDeserialization
{
    using Newtonsoft.Json;

    /// <summary>
    /// Holds the diagnostic information for the plan error
    /// </summary>
    internal class Diagnostic
    {
        /// <summary>
        /// Gets the resource address if known.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        [JsonProperty("address")]
        public string Address { get; private set; }

        /// <summary>
        /// Gets the detail.
        /// </summary>
        /// <value>
        /// The detail.
        /// </value>
        [JsonProperty("detail")]
        public string Detail { get; private set; }

        /// <summary>
        /// Gets the range, i.e. the start and end position of the issue within the script.
        /// </summary>
        /// <value>
        /// The range.
        /// </value>
        [JsonProperty("range")]
        public Range Range { get; private set; }

        /// <summary>
        /// Gets the severity.
        /// </summary>
        /// <value>
        /// The severity.
        /// </value>
        [JsonProperty("severity")]
        public string Severity { get; private set; }

        /// <summary>
        /// Gets the snippet containing the issue.
        /// </summary>
        /// <value>
        /// The snippet.
        /// </value>
        [JsonProperty("snippet")]
        public Snippet Snippet { get; private set; }

        /// <summary>
        /// Gets the summary.
        /// </summary>
        /// <value>
        /// The summary.
        /// </value>
        [JsonProperty("summary")]
        public string Summary { get; private set; }
    }
}