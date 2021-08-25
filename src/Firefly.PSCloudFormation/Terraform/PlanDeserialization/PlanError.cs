namespace Firefly.PSCloudFormation.Terraform.PlanDeserialization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Newtonsoft.Json;

    /// <summary>
    /// Top level object of JSON output from <c>Terraform plan</c>
    /// </summary>
    [DebuggerDisplay("{ErrorType}")]
    internal class PlanError
    {
        /// <summary>
        /// Maps error message headline to an enumerable type.
        /// </summary>
        private static readonly Dictionary<string, PlanErrorType> ErrorMessageToTypeMap = new Dictionary<string, PlanErrorType>
                                                                                       {
                                                                                           { "Error: Missing attribute separator", PlanErrorType.MissingAttributeSeparator },
                                                                                           { "Error: Missing required argument", PlanErrorType.MissingRequiredArgument },
                                                                                           { "Error: Value for unconfigurable attribute", PlanErrorType.UnconfigurableAtribute },
                                                                                           { "Error: Invalid or unknown key", PlanErrorType.InvalidOrUnknownKey }
                                                                                       };

        /// <summary>
        /// The message
        /// </summary>
        private string message;

        /// <summary>
        /// Gets the error diagnostics.
        /// </summary>
        /// <value>
        /// The diagnostic.
        /// </value>
        [JsonProperty("diagnostic")]
        public Diagnostic Diagnostic { get; private set; }

        /// <summary>
        /// Gets the type of the error.
        /// </summary>
        /// <value>
        /// The type of the error.
        /// </value>
        [JsonIgnore]
        public PlanErrorType ErrorType { get; private set; }

        /// <summary>
        /// Gets the level (e.g. "error").
        /// </summary>
        /// <value>
        /// The level.
        /// </value>
        [JsonProperty("@level")]
        public string Level { get; private set; }

        /// <summary>
        /// Gets the starting line number of the error within the HCL script.
        /// </summary>
        /// <value>
        /// The line number.
        /// </value>
        [JsonIgnore]
        public int LineNumber => this.Diagnostic.Range.Start.Line;

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        [JsonProperty("@message")]
        public string Message
        {
            get => this.message;

            private set
            {
                this.ErrorType = ErrorMessageToTypeMap.ContainsKey(value)
                                     ? ErrorMessageToTypeMap[value]
                                     : PlanErrorType.Unrecognized;
                this.message = value;
            }
        }

        /// <summary>
        /// Gets the module name.
        /// </summary>
        /// <value>
        /// The module.
        /// </value>
        [JsonProperty("@module")]
        public string Module { get; private set; }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        /// <value>
        /// The timestamp.
        /// </value>
        [JsonProperty("@timestamp")]
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("type")]
        public string Type { get; private set; }
    }
}