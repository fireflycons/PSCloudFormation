namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Newtonsoft.Json;

    /// <summary>
    /// Represents a resource from the state file.
    /// When importing from an existing AWS stack, the assumption is that the import
    /// will only find one resource instance per resource in the state.
    /// </summary>
    [DebuggerDisplay("{Address}")]
    internal class Resource
    {
        private List<ResourceInstance> instances;

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("provider")]
        public string Provider { get; set; }

        [JsonProperty("instances")]
        public List<ResourceInstance> Instances
        {
            get => this.instances;

            set
            {
                if (value.Count > 1)
                {
                    throw new InvalidOperationException(
                        "More than one instance per resource is unexpected when importing an AWS CloudFormation stack. Please raise an issue.");
                }

                this.instances = value;

                foreach (var instance in this.instances)
                {
                    instance.Parent = this;
                }
            }
        }

        [JsonIgnore]
        public ResourceInstance ResourceInstance => this.instances.FirstOrDefault();

        [JsonIgnore]
        public string Address => $"{this.Type}.{this.Name}";
    }
}