namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Firefly.EmbeddedResourceLoader;

    using YamlDotNet.Serialization;

    internal static class ResourceTraitsCollection
    {
        /// <summary>
        /// Initializes static members of the <see cref="ResourceTraitsCollection"/> class.
        /// </summary>
        static ResourceTraitsCollection()
        {
            using (var stream = new StreamReader(
                ResourceLoader.GetResourceStream("ResourceTraits.yaml", Assembly.GetCallingAssembly())))
            {
                var deserializer = new DeserializerBuilder().Build();

                var resourceGroups = deserializer.Deserialize<List<ResourceGroup>>(stream);

                ResourceTraits = new List<IResourceTraits>();

                foreach (var g in resourceGroups)
                {
                    ResourceTraits.AddRange(g.Resources);
                }

                TraitsAll = ResourceTraits.First(r => r.ResourceType == "all");
            }
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
        /// Gets the <see cref="IResourceTraits"/> with the specified resource type.
        /// </summary>
        /// <value>
        /// The <see cref="IResourceTraits"/>.
        /// </value>
        /// <param name="resourceType">Type of the resource.</param>
        /// <returns>An <see cref="IResourceTraits"/>.</returns>
        public static IResourceTraits Get(string resourceType)
        {
            var traits = ResourceTraits.FirstOrDefault(rt => rt.ResourceType == resourceType);

            return traits != null ? new ConsolitatedResourceTraits(TraitsAll, traits) : TraitsAll;
        }
    }
}