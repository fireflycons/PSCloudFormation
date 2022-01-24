﻿namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
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
    /// <seealso cref="ProviderResourceSchema" />
    internal class AwsSchema
    {
        /// <summary>
        /// Map of AWS -> Terraform resource names
        /// </summary>
        private static readonly List<ResourceTypeMapping> ResourceTypeMappings = new List<ResourceTypeMapping>();

        /// <summary>
        /// Map of Terraform resource type  to schema + traits
        /// </summary>
        private static readonly Dictionary<string, ResourceSchema> Schema = new Dictionary<string, ResourceSchema>();

        /// <summary>
        /// Gets the resource traits.
        /// </summary>
        /// <value>
        /// The resource traits.
        /// </value>
        private static readonly List<IResourceTraits> ResourceTraits = new List<IResourceTraits>();

        /// <summary>
        /// Initializes static members of the <see cref="AwsSchema"/> class.
        /// </summary>
        static AwsSchema()
        {
            LoadResourceTypeMappings();
            LoadResourceTraits();
            TraitsAll = ResourceTraits.First(r => r.ResourceType == "all");
            LoadSchema();
        }

        /// <summary>
        /// Gets traits shared by all resources.
        /// </summary>
        public static IResourceTraits TraitsAll { get; }

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

            return traits != null ? new ConsolidatedResourceTraits(TraitsAll, traits) : TraitsAll;
        }

        /// <summary>
        /// Loads the schema from the embedded resource.
        /// </summary>
        /// <returns>Entire aws provider schema.</returns>
        public static void LoadSchema()
        {
            using (var reader = new StreamReader(
                ResourceLoader.GetResourceStream("terraform-aws-schema.json", Assembly.GetExecutingAssembly())))
            using (var jsonReader = new JsonTextReader(reader))
            {
                foreach (var s in new JsonSerializer().Deserialize<Dictionary<string, ProviderResourceSchema>>(
                    jsonReader))
                {
                    Schema.Add(s.Key, new ResourceSchema(s.Key, s.Value, GetResourceTraits(s.Key)));
                }
            }
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
        private static void LoadResourceTraits()
        {
            using (var stream = new StreamReader(
                ResourceLoader.GetResourceStream("ResourceTraits.yaml", Assembly.GetCallingAssembly())))
            {
                var deserializer = new DeserializerBuilder().Build();

                var resourceGroups = deserializer.Deserialize<List<ResourceGroup>>(stream);

                foreach (var g in resourceGroups)
                {
                    ResourceTraits.AddRange(g.Resources);
                }
            }
        }

        /// <summary>
        /// Loads the map of terraform to AWS resource types
        /// </summary>
        private static void LoadResourceTypeMappings()
        {
            using (var reader = new StreamReader(
                ResourceLoader.GetResourceStream("terraform-resource-map.json", Assembly.GetExecutingAssembly())))
            using (var jsonReader = new JsonTextReader(reader))
            {
                ResourceTypeMappings.AddRange(new JsonSerializer().Deserialize<List<ResourceTypeMapping>>(jsonReader));
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
            if (Schema.ContainsKey(terraformType))
            {
                return Schema[terraformType];
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