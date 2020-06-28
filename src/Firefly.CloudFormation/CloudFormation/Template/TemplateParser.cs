﻿namespace Firefly.CloudFormation.CloudFormation.Template
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Base class for CLoudFormation template parsers.
    /// </summary>
    public abstract class TemplateParser : InputFileParser
    {
        /// <summary>
        /// Amount of padding to add to resource names to include random chars added by CloudFormation
        /// </summary>
        protected const int NestedStackPadWidth = 14;

        /// <summary>
        /// The parameter key name
        /// </summary>
        protected const string ParameterKeyName = "Parameters";

        /// <summary>
        /// The description key name
        /// </summary>
        protected const string DescriptionKeyName = "Description";

        /// <summary>
        /// The resource key name
        /// </summary>
        protected const string ResourceKeyName = "Resources";

        /// <summary>
        /// The nested stack type
        /// </summary>
        protected const string NestedStackType = "AWS::CloudFormation::Stack";

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateParser"/> class.
        /// </summary>
        /// <param name="templateBody">The template body.</param>
        protected TemplateParser(string templateBody)
            : base(templateBody)
        {
        }

        /// <summary>
        /// Creates a Cloud Formation template parser.
        /// </summary>
        /// <param name="templateBody">The template body.</param>
        /// <returns>A new <see cref="TemplateParser"/></returns>
        /// <exception cref="InvalidDataException">Template body is empty</exception>
        public static TemplateParser CreateParser(string templateBody)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (InputFileParser.GetInputFileFormat(templateBody))
            {
                case InputFileFormat.Json:

                    return new JsonTemplateParser(templateBody);

                case InputFileFormat.Yaml:

                    return new YamlTemplateParser(templateBody);

                default:

                    throw new InvalidDataException("Template body is empty");
            }
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <returns>List of <see cref="TemplateFileParameter"/></returns>
        public abstract IEnumerable<TemplateFileParameter> GetParameters();

        /// <summary>
        /// Gets the template description.
        /// </summary>
        /// <returns>Content of description property from template</returns>
        public abstract string GetTemplateDescription();

        /// <summary>
        /// Gets logical resource names of nested stacks declared in the given template
        /// Does not recurse these.
        /// </summary>
        /// <returns>List of nested stack logical resource names, if any.</returns>
        public IEnumerable<string> GetNestedStackNames()
        {
            return this.GetNestedStackNames(string.Empty);
        }

        /// <summary>
        /// Gets logical resource names of nested stacks declared in the given template, accounting for how CloudFormation will name them when the template runs.
        /// Does not recurse these.
        /// </summary>
        /// <param name="baseStackName">Name of the base stack</param>
        /// <returns>List of nested stack logical resource names, if any.</returns>
        public abstract IEnumerable<string> GetNestedStackNames(string baseStackName);

        /// <summary>
        /// Gets the logical resource names.
        /// </summary>
        /// <param name="stackName">Name of the parent stack. Used to prefix nested stack resources</param>
        /// <returns>List of resource names.</returns>
        public abstract IEnumerable<string> GetLogicalResourceNames(string stackName);
    }
}