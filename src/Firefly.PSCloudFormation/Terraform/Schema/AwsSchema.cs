namespace Firefly.PSCloudFormation.Terraform.Schema
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Firefly.EmbeddedResourceLoader;

    using Newtonsoft.Json;

    /// <summary>
    /// Represents the entire AWS schema as known to the terraform AWS provider.
    /// This is loaded from a JSON embedded resource that is generated from
    /// the provider source code.
    /// </summary>
    /// <seealso cref="ResourceSchema" />
    internal class AwsSchema : Dictionary<string, ResourceSchema>
    {
        /// <summary>
        /// Map of AWS -> Terraform resource names
        /// </summary>
        private static readonly List<ResourceTypeMapping> ResourceTypeMappings;

        /// <summary>
        /// Initializes static members of the <see cref="AwsSchema"/> class.
        /// </summary>
        static AwsSchema()
        {
            ResourceTypeMappings = LoadResourceTypeMappings();
        }

        /// <summary>
        /// Loads the schema from the embedded resource.
        /// </summary>
        /// <returns>Entire aws provider schema.</returns>
        public static AwsSchema LoadSchema()
        {
            using (var reader = new StreamReader(
                       ResourceLoader.GetResourceStream("terraform-aws-schema.json", Assembly.GetExecutingAssembly())))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return new JsonSerializer().Deserialize<AwsSchema>(jsonReader);
            }
        }

        /// <summary>
        /// Gets a resource schema given the name, which can be either the AWS or the Terraform name for the resource type.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>A <see cref="ResourceSchema"/> object.</returns>
        public ResourceSchema GetResourceSchema(string resourceName)
        {
            return resourceName.StartsWith("AWS::")
                       ? this.GetResourceSchemaByAwsName(resourceName)
                       : this.GetResourceSchemaByTerraformName(resourceName);
        }

        /// <summary>
        /// Loads the map of terraform to AWS resource types
        /// </summary>
        /// <returns>List of resource type mappings.</returns>
        private static List<ResourceTypeMapping> LoadResourceTypeMappings()
        {
            using (var reader = new StreamReader(
                       ResourceLoader.GetResourceStream(
                           "terraform-resource-map.json",
                           Assembly.GetExecutingAssembly())))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return new JsonSerializer().Deserialize<List<ResourceTypeMapping>>(jsonReader);
            }
        }

        /// <summary>
        /// Gets a resource schema given the AWS name for the resource type.
        /// </summary>
        /// <param name="awsName">Name of the resource.</param>
        /// <returns>A <see cref="ResourceSchema"/> object.</returns>
        private ResourceSchema GetResourceSchemaByAwsName(string awsName)
        {
            var mapping = ResourceTypeMappings.FirstOrDefault(m => m.Aws == awsName);

            if (mapping == null)
            {
                throw new KeyNotFoundException(
                    $"Resource \"{awsName}\": No corresponding Terraform resource found. If this is incorrect, please raise an issue.");
            }

            return this.GetResourceSchemaByTerraformName(mapping.Terraform);
        }

        /// <summary>
        /// Gets a resource schema given the Terraform name for the resource type.
        /// </summary>
        /// <param name="terraformName">Name of the resource.</param>
        /// <returns>A <see cref="ResourceSchema"/> object.</returns>
        private ResourceSchema GetResourceSchemaByTerraformName(string terraformName)
        {
            if (this.ContainsKey(terraformName))
            {
                return this[terraformName];
            }

            throw new KeyNotFoundException($"Resource \"{terraformName}\" not found.");
        }

        /// <summary>
        /// Maps an AWS resource type to equivalent Terraform resource.
        /// This is backed by the embedded resource <c>terraform-resource-map.json</c>
        /// </summary>
        [DebuggerDisplay("{Aws} -> {Terraform}")]
        private class ResourceTypeMapping
        {
            /// <summary>
            /// Gets or sets the AWS type name.
            /// </summary>
            /// <value>
            /// The AWS type name.
            /// </value>
            [JsonProperty("AWS")]
            public string Aws { get; set; }

            /// <summary>
            /// Gets or sets the terraform type name.
            /// </summary>
            /// <value>
            /// The terraform.
            /// </value>
            [JsonProperty("TF")]
            public string Terraform { get; set; }
        }
    }
}