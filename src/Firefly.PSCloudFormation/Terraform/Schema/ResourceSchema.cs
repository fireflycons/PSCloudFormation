namespace Firefly.PSCloudFormation.Terraform.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    /// <summary>
    /// Terraform schema for a resource
    /// </summary>
    internal class ResourceSchema
    {
        /// <summary>
        /// Gets or sets the resource attributes schema.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        [JsonProperty("Schema")]
        public Dictionary<string, ValueSchema> Attributes { get; set; }

        /// <summary>
        /// Gets an attribute schema given path to it.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>A <see cref="ValueSchema"/> for the attribute.</returns>
        /// <exception cref="System.ArgumentException">
        /// Value must be provided - path
        /// or
        /// Invalid attribute path \"{path}\" - path
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Invalid path \"{path}\": Attribute at \"{GetCurrentPathFromPathStack(processedPath)}\" is not a set or a list.
        /// or
        /// Invalid path \"{path}\": Attribute at \"{GetCurrentPathFromPathStack(processedPath)}\" is not a set or a list.
        /// </exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Resource does not contain an attribute \"{currentAttributeName}\".
        /// or
        /// Resource does not contain an attribute at\"{path}\".
        /// </exception>
        public ValueSchema GetAttributeByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Value must be provided", nameof(path));
            }

            if (path.StartsWith("*") || path.EndsWith("*"))
            {
                throw new ArgumentException("Invalid attribute path \"{path}\"", nameof(path));
            }

            ValueSchema currentAttribute = null;
            ValueSchema previousAttribute = null;
            var currentResource = this;
            var pathComponents = new Queue<string>(path.Split('.'));
            var processedPath = new Stack<string>();

            while (true)
            {
                var currentAttributeName = pathComponents.Dequeue();

                if (IsListIndexIndicator(currentAttributeName))
                {
                    processedPath.Push(currentAttributeName);
                    continue;
                }

                previousAttribute = currentAttribute;

                if (previousAttribute != null)
                {
                    // To get here, the previous attribute must be a set or list
                    if (!IsListIndexIndicator(processedPath.FirstOrDefault()))
                    {
                        throw new InvalidOperationException(
                            $"Invalid path \"{path}\": Attribute at \"{GetCurrentPathFromPathStack(processedPath)}\" is a set or a list. '*' was expected next in path.");
                    }
                }

                if (!currentResource.Attributes.ContainsKey(currentAttributeName))
                {
                    throw new KeyNotFoundException(
                        $"Resource does not contain an attribute \"{currentAttributeName}\".");
                }

                currentAttribute = currentResource.Attributes[currentAttributeName];

                processedPath.Push(currentAttributeName);

                if (pathComponents.Count == 0)
                {
                    return currentAttribute;
                }

                if (currentAttribute.Elem == null)
                {
                    // Path indicates Elem should be set
                    throw new KeyNotFoundException($"Resource does not contain an attribute at\"{path}\".");
                }

                if (!(currentAttribute.Elem is ResourceSchema nextResource))
                {
                    throw new InvalidOperationException(
                        $"Resource schema expected at \"{GetCurrentPathFromPathStack(processedPath)}\"");
                }

                currentResource = nextResource;
            }
        }

        private static string GetCurrentPathFromPathStack(IEnumerable<string> path)
        {
            var partialPath = path.ToList();
            partialPath.Reverse();
            return string.Join(".", partialPath);
        }

        private static bool IsListIndexIndicator(string pathComponent)
        {
            return pathComponent != null && (pathComponent == "*" || int.TryParse(pathComponent, out _));
        }
    }
}