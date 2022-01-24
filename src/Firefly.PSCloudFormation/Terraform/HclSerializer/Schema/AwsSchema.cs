namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Firefly.EmbeddedResourceLoader;

    using Newtonsoft.Json;

    using YamlDotNet.Serialization;

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
            ResourceTraits = LoadResourceTraits();
            TraitsAll = ResourceTraits.First(r => r.ResourceType == "all");
        }

        /// <summary>
        /// Gets traits shared by all resources.
        /// </summary>
        public static IResourceTraits TraitsAll { get; }

        /// <summary>
        /// Gets the resource traits.
        /// </summary>
        /// <value>
        /// The resource traits.
        /// </value>
        private static List<IResourceTraits> ResourceTraits { get; }

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
        /// Gets the <see cref="IResourceTraits"/> with the specified resource type.
        /// </summary>
        /// <value>
        /// The <see cref="IResourceTraits"/>.
        /// </value>
        /// <param name="resourceType">Type of the resource.</param>
        /// <returns>An <see cref="IResourceTraits"/>.</returns>
        public static IResourceTraits GetResourceTraits(string resourceType)
        {
            var traits = ResourceTraits.FirstOrDefault(rt => rt.ResourceType == resourceType);

            return traits != null ? new ConsolitatedResourceTraits(TraitsAll, traits) : TraitsAll;
        }

        /// <summary>
        /// Gets a resource schema given the name, which can be either the AWS or the Terraform name for the resource type.
        /// </summary>
        /// <param name="resourceType">Type of the resource.</param>
        /// <returns>A <see cref="ResourceSchema"/> object.</returns>
        public ResourceSchema GetResourceSchema(string resourceType)
        {
            return resourceType.StartsWith("AWS::")
                       ? this.GetResourceSchemaByAwsType(resourceType)
                       : this.GetResourceSchemaByTerraformType(resourceType);
        }

        /// <summary>
        /// Loads the additional resource traits that cannot be determined from the schema directly, or easily
        /// </summary>
        /// <returns>List of <see cref="IResourceTraits"/></returns>
        private static List<IResourceTraits> LoadResourceTraits()
        {
            using (var stream = new StreamReader(
                       ResourceLoader.GetResourceStream("ResourceTraits.yaml", Assembly.GetCallingAssembly())))
            {
                var deserializer = new DeserializerBuilder().Build();

                var resourceGroups = deserializer.Deserialize<List<ResourceGroup>>(stream);

                var traits = new List<IResourceTraits>();

                foreach (var g in resourceGroups)
                {
                    traits.AddRange(g.Resources);
                }

                return traits;
            }
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
        /// <param name="awsType">Name of the resource.</param>
        /// <returns>A <see cref="ResourceSchema"/> object.</returns>
        private ResourceSchema GetResourceSchemaByAwsType(string awsType)
        {
            var mapping = ResourceTypeMappings.FirstOrDefault(m => m.Aws == awsType);

            if (mapping == null)
            {
                throw new KeyNotFoundException(
                    $"Resource \"{awsType}\": No corresponding Terraform resource found. If this is incorrect, please raise an issue.");
            }

            return this.GetResourceSchemaByTerraformType(mapping.Terraform);
        }

        /// <summary>
        /// Gets a resource schema given the Terraform name for the resource type.
        /// </summary>
        /// <param name="terraformType">Name of the resource.</param>
        /// <returns>A <see cref="ResourceSchema"/> object.</returns>
        private ResourceSchema GetResourceSchemaByTerraformType(string terraformType)
        {
            if (this.ContainsKey(terraformType))
            {
                return this[terraformType];
            }

            throw new KeyNotFoundException($"Resource \"{terraformType}\" not found.");
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