namespace Firefly.CloudFormation.Parsers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Parser for JSON templates
    /// </summary>
    /// <seealso cref="TemplateParser" />
    internal class JsonTemplateParser : TemplateParser
    {
        /// <summary>
        /// The template
        /// </summary>
        private readonly JObject template;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTemplateParser"/> class.
        /// </summary>
        /// <param name="templateBody">The template body.</param>
        public JsonTemplateParser(string templateBody)
            : base(templateBody)
        {
            this.template = JObject.Parse(this.FileContent);

            if (!this.template.ContainsKey(TemplateParser.ResourceKeyName))
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

            if (!this.template.ContainsKey(TemplateParser.ParameterKeyName))
            {
                return parameters;
            }

            foreach (var param in this.template[ParameterKeyName].Cast<JProperty>())
            {
                var type = param.Children()["Type"].FirstOrDefault();

                if (type == null)
                {
                    throw new FormatException($"Parameter {param.Name} has no Type property");
                }

                parameters.Add(
                    new TemplateFileParameter
                        {
                            Name = param.Name,
                            Type = type.Value<string>(),
                            ConstraintDescription = GetNodeValue<string>(param, "ConstraintDescription"),
                            Description = GetNodeValue<string>(param, "Description"),
                            Default = GetNodeValue<string>(param, "Default"),
                            AllowedPattern = GetRegexNodeValue(param, "AllowedPattern"),
                            AllowedValues = GetArrayNodeValues(param, "AllowedValues"),
                            NoEcho = GetNodeValue<bool>(param, "NoEcho"),
                            MinLength = GetNodeValue<int>(param, "MinLength"),
                            MaxLength = GetNodeValue<int>(param, "MaxLength"),
                            MinValue = GetNodeValue<double>(param, "MinLength"),
                            MaxValue = GetNodeValue<double>(param, "MaxValue"),
                            HasMaxValue = HasNode(param, "MaxValue"),
                            HasMaxLength = HasNode(param, "MaxLength")
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
            return !this.template.ContainsKey(TemplateParser.DescriptionKeyName) ? string.Empty : this.template[DescriptionKeyName].Value<string>();
        }

        /// <summary>
        /// Gets logical resource names of nested stacks declared in the given template, accounting for how CLoudFormation will name them when the template runs.
        /// Does not recurse these.
        /// </summary>
        /// <param name="baseStackName">Name of the base stack</param>
        /// <returns>
        /// List of nested stack logical resource names, if any.
        /// </returns>
        /// <exception cref="FormatException">Resource {resource.Name} has no Type property</exception>
        public override IEnumerable<string> GetNestedStackNames(string baseStackName)
        {
            var nestedStacks = new List<string>();

            foreach (var resource in this.template[ResourceKeyName].Cast<JProperty>())
            {
                var type = resource.Children()["Type"].FirstOrDefault();

                if (type == null)
                {
                    throw new FormatException($"Resource {resource.Name} has no Type property");
                }

                if (type.Value<string>() == TemplateParser.NestedStackType)
                {
                    if (!string.IsNullOrEmpty(baseStackName))
                    {
                        nestedStacks.Add(baseStackName + "-" + resource.Name + string.Empty.PadRight(TemplateParser.NestedStackPadWidth));
                    }
                    else
                    {
                        nestedStacks.Add(resource.Name);
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
        /// <exception cref="FormatException">Resource {resource.Name} has no Type property</exception>
        public override IEnumerable<string> GetLogicalResourceNames(string stackName)
        {
            var resourceNames = new List<string> { stackName };

            foreach (var resource in this.template[ResourceKeyName].Cast<JProperty>())
            {
                var type = resource.Children()["Type"].FirstOrDefault();

                if (type == null)
                {
                    throw new FormatException($"Resource {resource.Name} has no Type property");
                }

                if (type.Value<string>() == TemplateParser.NestedStackType)
                {
                    resourceNames.Add(stackName + "-" + resource.Name + string.Empty.PadRight(TemplateParser.NestedStackPadWidth));
                }
                else
                {
                    resourceNames.Add(resource.Name);
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

            foreach (var resource in this.template[ResourceKeyName].Cast<JProperty>())
            {
                var type = resource.Children()["Type"].FirstOrDefault();
                var name = resource.Name;

                if (type == null)
                {
                    throw new FormatException($"Resource {name} has no Type property");
                }

                resources.Add(TemplateResource.Create(resource, name, type.Value<string>()));
            }

            return resources;
        }

        /// <summary>
        /// Saves the template to the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public override void Save(string path)
        {
            using (var sw = File.CreateText(path))
            using (var jw = new JsonTextWriter(sw))
            {
                jw.Formatting = Formatting.Indented;

                this.template.WriteTo(jw);
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
            using (var jw = new JsonTextWriter(sw))
            {
                jw.Formatting = Formatting.Indented;

                this.template.WriteTo(jw);

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
        private static bool HasNode(JToken parameterProperties, string keyName)
        {
            return parameterProperties.Children()[keyName].FirstOrDefault() != null;
        }

        /// <summary>
        /// Gets the array node value.
        /// </summary>
        /// <param name="parameterProperties">The parameter keys.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>Array of values</returns>
        private static string[] GetArrayNodeValues(JToken parameterProperties, string keyName)
        {
            var prop = parameterProperties.Children()[keyName].FirstOrDefault();

            return prop?.Children().Select(c => c.Value<string>()).ToArray();
        }

        /// <summary>
        /// Gets the node value.
        /// </summary>
        /// <typeparam name="T">Any <see cref="IConvertible"/> type</typeparam>
        /// <param name="parameterProperties">The parameter properties.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>Value of parameter node</returns>
        private static T GetNodeValue<T>(JToken parameterProperties, string keyName)
            where T : IConvertible
        {
            var prop = parameterProperties.Children()[keyName].FirstOrDefault();

            return prop != null ? prop.Value<T>() : default;
        }

        /// <summary>
        /// Gets the regex node value.
        /// </summary>
        /// <param name="parameterProperties">The parameter keys.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>Regex value</returns>
        private static Regex GetRegexNodeValue(JToken parameterProperties, string keyName)
        {
            var value = parameterProperties.Children()[keyName].FirstOrDefault()?.Value<string>();

            return value != null ? new Regex(value) : null;
        }
    }
}