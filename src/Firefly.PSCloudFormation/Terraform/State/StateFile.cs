namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;

    using QuikGraph;
    using QuikGraph.Graphviz;
    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Deserialization of the Terraform state file
    /// </summary>
    internal class StateFile
    {
        /// <summary>
        /// The serial number
        /// </summary>
        private int serial;

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the terraform version.
        /// </summary>
        /// <value>
        /// The terraform version.
        /// </value>
        [JsonProperty("terraform_version")]
        public string TerraformVersion { get; set; }

        /// <summary>
        /// Gets or sets the serial number.
        /// </summary>
        /// <value>
        /// The serial number which, when retrieved is incremented so that the next write of the file has a new serial number.
        /// </value>
        [JsonProperty("serial")]
        public int Serial
        {
            // Increment serial when writing out.
            get => this.serial + 1;
            set => this.serial = value;
        }

        /// <summary>
        /// Gets or sets the lineage.
        /// </summary>
        /// <value>
        /// The lineage.
        /// </value>
        [JsonProperty("lineage")]
        public Guid Lineage { get; set; }

        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        [JsonProperty("resources")]
        public List<ResourceDeclaration> Resources { get; set; }

        /// <summary>
        /// Saves the state file to the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public void Save(string path)
        {
            if (File.Exists(path))
            {
                var backup = $"{path}.backup";

                if (File.Exists(backup))
                {
                    File.Delete(backup);
                    File.Move(path, backup);
                }

                File.Delete(path);
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented), new UTF8Encoding(false));
        }
    }
}