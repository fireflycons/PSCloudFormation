namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
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
        /// Schema for "id", meta-argument which is implicit on all resources.
        /// </summary>
        private static readonly ValueSchema IdentitySchema = new ValueSchema
                                                                 {
                                                                     Type = SchemaValueType.TypeString,
                                                                     ConfigMode = SchemaConfigMode.SchemaConfigModeAuto,
                                                                     Computed = true
                                                                 };

        /// <summary>
        /// Schema for "timeouts" meta-argument which is present on some resources.
        /// </summary>
        private static readonly ValueSchema TimeoutsSchema = new ValueSchema
                                                                 {
                                                                     Type = SchemaValueType.TypeObject,
                                                                     ConfigMode = SchemaConfigMode.SchemaConfigModeAuto,
                                                                     Optional = true,
                                                                     Elem = new ResourceSchema
                                                                                {
                                                                                    Attributes =
                                                                                        new
                                                                                        Dictionary<string, ValueSchema>
                                                                                            {
                                                                                                {
                                                                                                    "create",
                                                                                                    new ValueSchema
                                                                                                        {
                                                                                                            Type =
                                                                                                                SchemaValueType
                                                                                                                    .TypeString,
                                                                                                            ConfigMode =
                                                                                                                SchemaConfigMode
                                                                                                                    .SchemaConfigModeAuto,
                                                                                                            Optional =
                                                                                                                true
                                                                                                        }
                                                                                                },
                                                                                                {
                                                                                                    "update",
                                                                                                    new ValueSchema
                                                                                                        {
                                                                                                            Type =
                                                                                                                SchemaValueType
                                                                                                                    .TypeString,
                                                                                                            ConfigMode =
                                                                                                                SchemaConfigMode
                                                                                                                    .SchemaConfigModeAuto,
                                                                                                            Optional =
                                                                                                                true
                                                                                                        }
                                                                                                },
                                                                                                {
                                                                                                    "delete",
                                                                                                    new ValueSchema
                                                                                                        {
                                                                                                            Type =
                                                                                                                SchemaValueType
                                                                                                                    .TypeString,
                                                                                                            ConfigMode =
                                                                                                                SchemaConfigMode
                                                                                                                    .SchemaConfigModeAuto,
                                                                                                            Optional =
                                                                                                                true
                                                                                                        }
                                                                                                },
                                                                                                {
                                                                                                    "read",
                                                                                                    new ValueSchema
                                                                                                        {
                                                                                                            Type =
                                                                                                                SchemaValueType
                                                                                                                    .TypeString,
                                                                                                            ConfigMode =
                                                                                                                SchemaConfigMode
                                                                                                                    .SchemaConfigModeAuto,
                                                                                                            Optional =
                                                                                                                true
                                                                                                        }
                                                                                                },
                                                                                                {
                                                                                                    "default",
                                                                                                    new ValueSchema
                                                                                                        {
                                                                                                            Type =
                                                                                                                SchemaValueType
                                                                                                                    .TypeString,
                                                                                                            ConfigMode =
                                                                                                                SchemaConfigMode
                                                                                                                    .SchemaConfigModeAuto,
                                                                                                            Optional =
                                                                                                                true
                                                                                                        }
                                                                                                }
                                                                                            }
                                                                                }
                                                                 };

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

            if (IsListIndexIndicator(path.First().ToString()) || IsListIndexIndicator(path.Last().ToString(), true))
            {
                throw new ArgumentException($"Invalid attribute path \"{path}\"", nameof(path));
            }

            if (path == "id")
            {
                // 'id' is not stored in the schema because _everything_ has it
                return IdentitySchema;
            }

            ValueSchema currentAttribute = null;
            var currentResource = this;
            var pathComponents = new Queue<string>(AttributePath.Split(path));
            var processedPath = new Stack<string>();

            while (true)
            {
                var currentAttributeName = pathComponents.Dequeue();

                if (IsListIndexIndicator(currentAttributeName))
                {
                    processedPath.Push(currentAttributeName);
                    continue;
                }

                var previousAttribute = currentAttribute;

                if (previousAttribute != null && previousAttribute.Type != SchemaValueType.TypeObject)
                {
                    // To get here, the previous attribute must be a set or list
                    if (!IsListIndexIndicator(processedPath.FirstOrDefault()))
                    {
                        throw new InvalidOperationException(
                            $"Invalid path \"{path}\": Attribute at \"{GetCurrentPathFromPathStack()}\" is a set or a list. List indicator ('*', '#' or integer) was expected next in path.");
                    }
                }

                if (currentAttributeName == "timeouts")
                {
                    currentAttribute = TimeoutsSchema;
                }
                else
                {
                    if (!currentResource.Attributes.ContainsKey(currentAttributeName))
                    {
                        throw CreateKeyNotFoundException();
                    }

                    currentAttribute = currentResource.Attributes[currentAttributeName];
                }

                processedPath.Push(currentAttributeName);

                if (pathComponents.Count == 0)
                {
                    return currentAttribute;
                }

                switch (currentAttribute.Elem)
                {
                    case ResourceSchema resourceSchema:

                        currentResource = resourceSchema;
                        break;

                    case ValueSchema valueSchema when pathComponents.Count == 1 && currentAttribute.Type == SchemaValueType.TypeMap:

                        return valueSchema;

                    default:

                        // If Elem is null or contains a value schema, then there are no further path components
                        // below this attribute.
                        throw CreateKeyNotFoundException();
                }
            }

            KeyNotFoundException CreateKeyNotFoundException()
            {
                return new KeyNotFoundException($"Resource does not contain an attribute at\"{path}\".");
            }

            string GetCurrentPathFromPathStack()
            {
                return string.Join(".", processedPath.Reverse().ToList());
            }
        }

        /// <summary>
        /// Determines whether [is list index indicator] [the specified path component].
        /// </summary>
        /// <param name="pathComponent">The path component.</param>
        /// <param name="ignoreDigits">if set to <c>true</c> [ignore digits].</param>
        /// <returns>
        ///   <c>true</c> if [is list index indicator] [the specified path component]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsListIndexIndicator(string pathComponent, bool ignoreDigits = false)
        {
            var isListIndicator = pathComponent != null
                   && (pathComponent == "*" || pathComponent == "#");

            if (ignoreDigits)
            {
                return isListIndicator;
            }

            return isListIndicator || int.TryParse(pathComponent, out _);
        }
    }
}