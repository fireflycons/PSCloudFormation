namespace Firefly.PSCloudFormation.Terraform.PlanDeserialization
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;

    /// <summary>
    /// Gets the code snippet containing the error
    /// </summary>
    internal class Snippet
    {
        /// <summary>
        /// Gets the snippet code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        [JsonProperty("code")]
        public string Code { get; private set; }

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        [JsonProperty("context")]
        public string Context { get; private set; }

        /// <summary>
        /// Gets the highlight end offset for highlighting the issue within the snippet.
        /// </summary>
        /// <value>
        /// The highlight end offset.
        /// </value>
        [JsonProperty("highlight_end_offset")]
        public int HighlightEndOffset { get; private set; }

        /// <summary>
        /// Gets the highlight start offset for highlighting the issue within the snippet.
        /// </summary>
        /// <value>
        /// The highlight start offset.
        /// </value>
        [JsonProperty("highlight_start_offset")]
        public int HighlightStartOffset { get; private set; }

        /// <summary>
        /// Gets the starting line number of the snippet.
        /// </summary>
        /// <value>
        /// The start line.
        /// </value>
        [JsonProperty("start_line")]
        public int StartLine { get; private set; }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        [JsonProperty("values")]
        public List<object> Values { get; private set; }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            return this.HighlightStartOffset ^ HashString(this.Code) ^ this.HighlightEndOffset ^ HashString(this.Context) ^ this.StartLine;
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

        /// <summary>
        /// Simple string hash that is consistent where string.GetHashCode seems not to be.
        /// </summary>
        /// <param name="str">String to be hashed.</param>
        /// <returns>Hash of the string</returns>
        private static int HashString(string str)
        {
            return Encoding.UTF8.GetBytes(str).Aggregate(0, (current, b) => (37 * current) + b);
        }
    }
}