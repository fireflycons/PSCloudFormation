namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Firefly.EmbeddedResourceLoader;

    using YamlDotNet.Serialization;

    internal class ResourceTraitsCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceTraitsCollection"/> class.
        /// </summary>
        /// <param name="resourceGroups">The resource groups.</param>
        private ResourceTraitsCollection(List<ResourceGroup> resourceGroups)
        {
            // Flatten resource list. Grouping only for legibility in the YAML
            this.ResourceTraits = new List<ResourceTraits>();

            foreach (var g in resourceGroups)
            {
                this.ResourceTraits.AddRange(g.Resources);
            }

            this.TraitsAll = this.ResourceTraits.First(r => r.ResourceType == "all");
        }

        /// <summary>
        /// Gets traits shared by all resources.
        /// </summary>
        public ResourceTraits TraitsAll { get; }

        /// <summary>
        /// Gets the resource traits.
        /// </summary>
        /// <value>
        /// The resource traits.
        /// </value>
        private List<ResourceTraits> ResourceTraits { get; }

        /// <summary>
        /// Gets the <see cref="IResourceTraits"/> with the specified resource type.
        /// </summary>
        /// <value>
        /// The <see cref="IResourceTraits"/>.
        /// </value>
        /// <param name="resourceType">Type of the resource.</param>
        /// <returns>An <see cref="IResourceTraits"/>.</returns>
        public IResourceTraits this[string resourceType]
        {
            get
            {
                var traits = this.ResourceTraits.FirstOrDefault(rt => rt.ResourceType == resourceType);

                if (traits != null)
                {
                    return new ConsolitatedResourceTraits(this.TraitsAll, traits);
                }

                return this.TraitsAll;
            }
        }

        /// <summary>
        /// Loads this instance.
        /// </summary>
        /// <returns>A <see cref="ResourceTraitsCollection"/> representing the YAML traits embedded resource.</returns>
        public static ResourceTraitsCollection Load()
        {
            using (var stream = new StreamReader(
                ResourceLoader.GetResourceStream("ResourceTraits.yaml", Assembly.GetCallingAssembly())))
            {
                var deserializer = new DeserializerBuilder().Build();

                return new ResourceTraitsCollection(deserializer.Deserialize<List<ResourceGroup>>(stream));
            }
        }
    }
}