namespace Firefly.CloudFormation.Parsers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using YamlDotNet.RepresentationModel;

    /// <summary>
    /// Parser for YAML templates
    /// </summary>
    internal class YamlTemplateParser : TemplateParser
    {
        /// <summary>
        /// Lookup of parameter node keys
        /// </summary>
        private readonly Dictionary<string, YamlScalarNode> propertyKeys =
            new[]
                {
                    ResourceKeyName, ParameterKeyName, "Type", "Description", "Default", "AllowedValues",
                    "AllowedPattern", "NoEcho", "ConstraintDescription", "MinLength", "MaxLength", "MinValue",
                    "MaxValue"
                }.ToDictionary(key => key, key => new YamlScalarNode(key));

        /// <summary>
        /// The YAML representation
        /// </summary>
        private readonly YamlStream yaml = new YamlStream();

        /// <summary>
        /// The root node
        /// </summary>
        private readonly YamlMappingNode rootNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlTemplateParser"/> class.
        /// </summary>
        /// <param name="templateBody">The template body.</param>
        public YamlTemplateParser(string templateBody)
            : base(templateBody)
        {
            using (var sr = new StringReader(this.FileContent))
            {
                this.yaml.Load(sr);
            }

            this.rootNode = (YamlMappingNode)this.yaml.Documents[0].RootNode;

            if (this.rootNode == null)
            {
                throw new FormatException("Template body is empty");
            }

            if (!this.rootNode.Children.ContainsKey(this.propertyKeys[ResourceKeyName]))
            {
                throw new FormatException("Illegal template: No Resources block found.");
            }
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <returns>
        /// List of <see cref="TemplateFileParameter" />
        /// </returns>
        /// <exception cref="FormatException">Parameter <c>param.Name</c> has no Type property</exception>
        public override IEnumerable<TemplateFileParameter> GetParameters()
        {
            var parameters = new List<TemplateFileParameter>();

            if (!this.rootNode.Children.ContainsKey(this.propertyKeys[ParameterKeyName]))
            {
                return parameters;
            }

            var parameterBlock = (YamlMappingNode)this.rootNode.Children[this.propertyKeys[ParameterKeyName]];

            foreach (var parameterNode in parameterBlock.Children)
            {
                var paramName = ((YamlScalarNode)parameterNode.Key).Value;
                var param = ((YamlMappingNode)parameterNode.Value).Children;

                if (!param.ContainsKey(this.propertyKeys["Type"]))
                {
                    throw new FormatException($"Parameter {paramName} has no Type property");
                }

                parameters.Add(
                    new TemplateFileParameter
                        {
                            Name = paramName,
                            Type = this.GetNodeValue<string>(param, "Type"),
                            ConstraintDescription = this.GetNodeValue<string>(param, "ConstraintDescription"),
                            Description = this.GetNodeValue<string>(param, "Description"),
                            Default = this.GetNodeValue<string>(param, "Default"),
                            AllowedPattern = this.GetRegexNodeValue(param, "AllowedPattern"),
                            AllowedValues = this.GetArrayNodeValues(param, "AllowedValues"),
                            NoEcho = this.GetNodeValue<bool>(param, "NoEcho"),
                            MinLength = this.GetNodeValue<int>(param, "MinLength"),
                            MaxLength = this.GetNodeValue<int>(param, "MaxLength"),
                            MinValue = this.GetNodeValue<double>(param, "MinLength"),
                            MaxValue = this.GetNodeValue<double>(param, "MaxValue"),
                            HasMaxValue = this.HasNode(param, "MaxValue"),
                            HasMaxLength = this.HasNode(param, "MaxLength")
                    });
            }

            return parameters;
        }

        /// <summary>
        /// Gets the template description.
        /// </summary>
        /// <returns>
        /// Content of description property from template
        /// </returns>
        public override string GetTemplateDescription()
        {
            var descriptionKey = new YamlScalarNode(DescriptionKeyName);

            return !this.rootNode.Children.ContainsKey(descriptionKey)
                       ? string.Empty
                       : this.GetNodeValue<string>(this.rootNode.Children, DescriptionKeyName);
        }

        /// <summary>
        /// Gets logical resource names of nested stacks declared in the given template, accounting for how CloudFormation will name them when the template runs.
        /// Does not recurse these.
        /// </summary>
        /// <param name="baseStackName">Name of the base stack</param>
        /// <returns>
        /// List of nested stack logical resource names, if any.
        /// </returns>
        /// <exception cref="FormatException">Resource {resourceName} has no Type property</exception>
        public override IEnumerable<string> GetNestedStackNames(string baseStackName)
        {
            var nestedStacks = new List<string>();

            var resourceBlock = (YamlMappingNode)this.rootNode.Children[this.propertyKeys[ResourceKeyName]];

            foreach (var resourceNode in resourceBlock.Children)
            {
                var resourceName = ((YamlScalarNode)resourceNode.Key).Value;
                var resource = ((YamlMappingNode)resourceNode.Value).Children;

                if (!resource.ContainsKey(this.propertyKeys["Type"]))
                {
                    throw new FormatException($"Resource {resourceName} has no Type property");
                }

                var type = (YamlScalarNode)resource[this.propertyKeys["Type"]];

                if (type.Value == NestedStackType)
                {
                    if (!string.IsNullOrEmpty(baseStackName))
                    {
                        nestedStacks.Add(baseStackName + "-" + resourceName + string.Empty.PadRight(NestedStackPadWidth));
                    }
                    else
                    {
                        nestedStacks.Add(resourceName);
                    }
                }
            }

            return nestedStacks;
        }

        /// <summary>
        /// Gets the logical resource names.
        /// </summary>
        /// <param name="stackName">Name of the parent stack. Used to prefix nested stack resources</param>
        /// <returns>
        /// List of resource names.
        /// </returns>
        /// <exception cref="FormatException">Resource {resourceName} has no Type property</exception>
        public override IEnumerable<string> GetLogicalResourceNames(string stackName)
        {
            var resourceNames = new List<string> { stackName };

            var resourceBlock = (YamlMappingNode)this.rootNode.Children[this.propertyKeys[ResourceKeyName]];

            foreach (var resourceNode in resourceBlock.Children)
            {
                var resourceName = ((YamlScalarNode)resourceNode.Key).Value;
                var resource = ((YamlMappingNode)resourceNode.Value).Children;

                if (!resource.ContainsKey(this.propertyKeys["Type"]))
                {
                    throw new FormatException($"Resource {resourceName} has no Type property");
                }

                var type = (YamlScalarNode)resource[this.propertyKeys["Type"]];

                if (type.Value == NestedStackType)
                {
                    resourceNames.Add(stackName + "-" + resourceName + string.Empty.PadRight(NestedStackPadWidth));
                }
                else
                {
                    resourceNames.Add(resourceName);
                }
            }

            return resourceNames;
        }

        /// <summary>
        /// Gets the template resources.
        /// </summary>
        /// <returns>
        /// Enumerable of resources found in template
        /// </returns>
        public override IEnumerable<TemplateResource> GetResources()
        {
            var resources = new List<TemplateResource>();
            var resourceBlock = (YamlMappingNode)this.rootNode.Children[this.propertyKeys[ResourceKeyName]];

            foreach (var resourceNode in resourceBlock.Children)
            {
                var resourceName = ((YamlScalarNode)resourceNode.Key).Value;
                var resource = ((YamlMappingNode)resourceNode.Value).Children;

                if (!resource.ContainsKey(this.propertyKeys["Type"]))
                {
                    throw new FormatException($"Resource {resourceName} has no Type property");
                }

                var type = (YamlScalarNode)resource[this.propertyKeys["Type"]];

                resources.Add(TemplateResource.Create(resourceNode.Value, resourceName, type.Value));
            }

            return resources;
        }

        /// <summary>
        /// Saves the template to the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public override void Save(string path)
        {
            using (var sw = new StringWriter())
            {
                this.yaml.Save(sw, false);
                File.WriteAllText(path, sw.ToString(), new UTF8Encoding(false));
            }
        }

        /// <summary>
        /// Gets the template by re-serializing the current state of the representation model.
        /// </summary>
        /// <returns>
        /// Template body as string
        /// </returns>
        public override string GetTemplate()
        {
            using (var sw = new StringWriter())
            {
                this.yaml.Save(sw, false);
                return sw.ToString();
            }
        }

        /// <summary>
        /// Determines whether the specified parameter properties has node.
        /// </summary>
        /// <param name="parameterProperties">The parameter properties.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>
        ///   <c>true</c> if the specified parameter properties has node; otherwise, <c>false</c>.
        /// </returns>
        private bool HasNode(IDictionary<YamlNode, YamlNode> parameterProperties, string keyName)
        {
            return parameterProperties.ContainsKey(this.propertyKeys[keyName]);
        }

        /// <summary>
        /// Gets the array node value.
        /// </summary>
        /// <param name="parameterProperties">The parameter keys.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>Array of values</returns>
        /// <exception cref="FormatException">Unexpected node type {values.GetType().Name}</exception>
        private string[] GetArrayNodeValues(IDictionary<YamlNode, YamlNode> parameterProperties, string keyName)
        {
            if (parameterProperties.ContainsKey(this.propertyKeys[keyName]))
            {
                var values = parameterProperties[this.propertyKeys[keyName]];

                switch (values)
                {
                    case YamlScalarNode scalarNode:
                        return new[] { scalarNode.Value };

                    case YamlSequenceNode sequenceNode:
                        return sequenceNode.Children.Cast<YamlScalarNode>().Select(n => n.Value).ToArray();

                    default:
                        throw new FormatException($"Unexpected node type {values.GetType().Name}");
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the node value.
        /// </summary>
        /// <typeparam name="T">Any <see cref="IConvertible"/> type</typeparam>
        /// <param name="parameterProperties">The parameter properties.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>Value of parameter node</returns>
        private T GetNodeValue<T>(IDictionary<YamlNode, YamlNode> parameterProperties, string keyName)
            where T : IConvertible
        {
            if (!parameterProperties.ContainsKey(this.propertyKeys[keyName]))
            {
                return default;
            }

            var val = ((YamlScalarNode)parameterProperties[this.propertyKeys[keyName]]).Value;

            return (T)Convert.ChangeType(val, typeof(T), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the regex node value.
        /// </summary>
        /// <param name="parameterProperties">The parameter keys.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>Regex value</returns>
        private Regex GetRegexNodeValue(IDictionary<YamlNode, YamlNode> parameterProperties, string keyName)
        {
            return parameterProperties.ContainsKey(this.propertyKeys[keyName])
                       ? new Regex(((YamlScalarNode)parameterProperties[this.propertyKeys[keyName]]).Value)
                       : null;
        }
    }
}