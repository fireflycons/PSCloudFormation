namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Deserialization of the Terraform state file
    /// </summary>
    internal class StateFile
    {
        /// <summary>
        /// The state file name
        /// </summary>
        private const string StateFileName = "terraform.tfstate";

        /// <summary>
        /// Regex to find last index on a JSON path
        /// </summary>
        private static readonly Regex IndexRegex = new Regex(@"\[(?<index>\d+)\]$");

        /// <summary>
        /// The serial number
        /// </summary>
        private int serial;

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
        public List<StateFileResourceDeclaration> Resources { get; set; }

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
        /// Gets or sets the terraform version.
        /// </summary>
        /// <value>
        /// The terraform version.
        /// </value>
        [JsonProperty("terraform_version")]
        public string TerraformVersion { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// Updates the external state file (i.e. <c>terraform.tfstate</c>).
        /// </summary>
        /// <param name="changes">List of changes to make to state file.</param>
        /// <remarks>
        /// Assumes the working directory is set to the terraform workspace
        /// </remarks>
        /// <returns><c>true</c> if the state file was modified.</returns>
        public static async Task<bool> UpdateExternalStateFileAsync(IEnumerable<StateFileModification> changes)
        {
            var stateFile = JsonConvert.DeserializeObject<StateFile>(await AsyncFileHelpers.ReadAllTextAsync(StateFileName));
            var modified = false;

            foreach (var change in changes)
            {
                var resource = stateFile.Resources.FirstOrDefault(r => r.Module == change.Module && r.Name == change.ResourceName);
                var targetValue = resource?.ResourceInstance?.Attributes.SelectToken(change.AttributePath);

                if (targetValue == null)
                {
                    continue;
                }

                switch (targetValue.Parent)
                {
                    case JProperty jp:

                        jp.Value = change.NewValue;
                        modified = true;
                        break;

                    case JArray ja:
                        {
                            // Index is last indexer on the provided path
                            var m = IndexRegex.Match(change.AttributePath);

                            if (!m.Success)
                            {
                                return false;
                            }

                            var index = int.Parse(m.Groups["index"].Value);

                            ja[index] = change.NewValue;
                            modified = true;
                            break;
                        }
                }
            }

            if (modified)
            {
                await stateFile.SaveAsync(StateFileName);
            }

            return modified;
        }

        /// <summary>
        /// Gets resources filtered by module name.
        /// </summary>
        /// <param name="moduleName">Name of the module.</param>
        /// <returns>Filtered resources.</returns>
        public IEnumerable<StateFileResourceDeclaration> FilteredResources(string moduleName)
        {
            return string.IsNullOrEmpty(moduleName)
                       ? this.Resources.Where(r => r.Module == null) // root module
                       : this.Resources.Where(r => r.Module == $"module.{moduleName}");
        }

        /// <summary>
        /// Saves the state file to the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Task to await.</returns>
        public async Task SaveAsync(string path)
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

            await AsyncFileHelpers.WriteAllTextAsync(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}