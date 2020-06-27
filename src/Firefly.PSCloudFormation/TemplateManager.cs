namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormation.CloudFormation;
    using Firefly.CloudFormation.CloudFormation.Template;
    using Firefly.PowerShell.DynamicParameters;

    /// <summary>
    /// Utility class to parse out stack parameters and convert them to PowerShell dynamic parameters
    /// </summary>
    public class TemplateManager
    {
        /// <summary>
        /// Map of AWS-Specific parameter type to validation regex
        /// </summary>
        private static readonly Dictionary<string, Regex> AwsParameterTypeRegexes = new Dictionary<string, Regex>
                                                                                        {
                                                                                            {
                                                                                                "AWS::EC2::AvailabilityZone::Name",
                                                                                                new Regex(
                                                                                                    @"^\w{2}-\w+-(\w+-)?\d[a-f]$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::EC2::Image::Id",
                                                                                                new Regex(
                                                                                                    @"^ami-([0-9a-f]{8}|[0-9a-f]{17})$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::EC2::Instance::Id",
                                                                                                new Regex(
                                                                                                    @"^i-([0-9a-f]{8}|[0-9a-f]{17})$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::EC2::KeyPair::KeyName",
                                                                                                new Regex(
                                                                                                    @"^\w{1,255}$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::EC2::SecurityGroup::GroupName",
                                                                                                new Regex(
                                                                                                    @"^[\sa-zA-Z0-9_\-\.\:\/\(\}\#\,\@\[\]\+\=\&\;\{\}\!\$\*]{1-255}$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::EC2::SecurityGroup::Id",
                                                                                                new Regex(
                                                                                                    @"^sg-([0-9a-f]{8}|[0-9a-f]{17})$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::EC2::Subnet::Id",
                                                                                                new Regex(
                                                                                                    @"^subnet-([0-9a-f]{8}|[0-9a-f]{17})$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::EC2::Volume::Id",
                                                                                                new Regex(
                                                                                                    @"^vol-([0-9a-f]{8}|[0-9a-f]{17})$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::EC2::VPC::Id",
                                                                                                new Regex(
                                                                                                    @"^vpc-([0-9a-f]{8}|[0-9a-f]{17})$")
                                                                                            },
                                                                                            {
                                                                                                "AWS::Route53::HostedZone::Id",
                                                                                                new Regex(
                                                                                                    @"^Z[0-9A-Z]+$")
                                                                                            }
                                                                                        };

        /// <summary>
        /// The stack operation
        /// </summary>
        private readonly StackOperation stackOperation;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateManager"/> class.
        /// </summary>
        /// <param name="templateResolver">The template resolver.</param>
        /// <param name="stackOperation">Stack operation being performed</param>
        public TemplateManager(IInputFileResolver templateResolver, StackOperation stackOperation)
        {
            this.stackOperation = stackOperation;
            this.TemplateParameters =
                TemplateParser.CreateParser(templateResolver.FileContent).GetParameters().ToList();
        }

        /// <summary>
        /// Gets the template parameters.
        /// </summary>
        /// <value>
        /// The template parameters.
        /// </value>
        public List<TemplateFileParameter> TemplateParameters { get; }

        /// <summary>
        /// Gets the stack dynamic parameters.
        /// </summary>
        /// <returns>A <see cref="RuntimeDefinedParameterDictionary"/> containing all non-SSM template parameters</returns>
        public RuntimeDefinedParameterDictionary GetStackDynamicParameters()
        {
            var dynamicParams = new RuntimeDefinedParameterDictionaryHelper();

            foreach (var param in this.TemplateParameters.Where(p => !p.IsSsmParameter))
            {
                var builder = new RuntimeDefinedParameterBuilder(param.Name, GetClrTypeFromAwsType(param.Type));

                if (param.Default == null && this.stackOperation == StackOperation.Create)
                {
                    // Only make parameter mandatory when creating.
                    builder.WithMandatory();
                }

                if (!string.IsNullOrEmpty(param.Description))
                {
                    builder.WithHelpMessage(param.Description);
                }

                if (param.Type.Contains("AWS::"))
                {
                    // Set a ValidatePattern for the given AWS type
                    var baseType = param.Type;
                    if (param.Type.StartsWith("List<"))
                    {
                        baseType = Regex.Match(param.Type, @"List<(?<baseType>.*)>").Groups["baseType"].Value;
                    }

                    builder.WithValidatePattern(AwsParameterTypeRegexes[baseType]);
                }
                else
                {
                    if (param.AllowedValues != null && param.AllowedValues.Any())
                    {
                        builder.WithValidateSet(param.AllowedValues);
                    }

                    if (param.Type == "String")
                    {
                        if (param.AllowedPattern != null)
                        {
                            builder.WithValidatePattern(param.AllowedPattern);
                        }

                        if (param.HasMaxLength)
                        {
                            builder.WithValidateLength(param.MinLength, param.MaxLength);
                        }
                    }

                    if ((param.Type == "Number" || param.Type == "List<Number>") && param.HasMaxValue)
                    {
                        builder.WithValidateRange(param.MinValue, param.MaxValue);
                    }
                }

                var dynamicParameter = builder.Build();

                dynamicParams.Add(dynamicParameter);
            }

            return (RuntimeDefinedParameterDictionary)dynamicParams;
        }

        /// <summary>
        /// Gets the CLR type for a parameter given the type specified for it in the template.
        /// </summary>
        /// <param name="awsType">Type of the AWS template parameter.</param>
        /// <returns>CLR type</returns>
        private static Type GetClrTypeFromAwsType(string awsType)
        {
            switch (awsType)
            {
                case "Number":
                
                    return typeof(double);

                case "List<Number>":
                    
                    return typeof(double[]);
            }

            if (awsType == "CommaDelimitedList" || awsType.StartsWith("List<"))
            {
                // Any remaining lists are list of AWS-Specific parameter types, which are all strings
                return typeof(string[]);
            }

            // Everything else is string
            return typeof(string);
        }
    }
}