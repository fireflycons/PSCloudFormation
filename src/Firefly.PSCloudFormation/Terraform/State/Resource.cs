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
        /// <summary>
        /// The instances of this resource. When importing from a CloudFormation stack, there should only be one instance per resource.
        /// </summary>
        private List<ResourceInstance> instances;

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        /// <value>
        /// The provider.
        /// </value>
        [JsonProperty("provider")]
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the instances.
        /// </summary>
        /// <value>
        /// The instances.
        /// </value>
        /// <exception cref="System.InvalidOperationException">More than one instance per resource is unexpected when importing an AWS CloudFormation stack. Please raise an issue.</exception>
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

        /// <summary>
        /// Gets the resource instance.
        /// </summary>
        /// <value>
        /// The resource instance.
        /// </value>
        [JsonIgnore]
        public ResourceInstance ResourceInstance => this.instances.FirstOrDefault();

        /// <summary>
        /// Gets the resource instance address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        [JsonIgnore]
        public string Address => $"{this.Type}.{this.Name}";
    }
}