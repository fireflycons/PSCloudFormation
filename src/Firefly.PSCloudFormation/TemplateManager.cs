namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Parsers;
    using Firefly.CloudFormation.Resolvers;
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
                                                                                                    @"^[^\s].{1,253}[^\s]$")
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
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateManager"/> class.
        /// </summary>
        /// <param name="templateResolver">The template resolver.</param>
        /// <param name="stackOperation">Stack operation being performed</param>
        /// <param name="logger">Logger to send error messages to.</param>
        public TemplateManager(IInputFileResolver templateResolver, StackOperation stackOperation, ILogger logger)
        {
            this.logger = logger;
            this.stackOperation = stackOperation;

            try
            {
                this.TemplateParameters =
                    TemplateParser.Create(templateResolver.FileContent).GetParameters().ToList();
            }
            catch (Exception e)
            {
                this.logger?.LogError($"Error parsing CloudFormation Template: {e.Message}");
                this.logger?.LogError(e.StackTrace);
                throw;
            }
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
        /// <param name="fileParameters">Parameters read from parameter file, if any.</param>
        /// <returns>A <see cref="RuntimeDefinedParameterDictionary"/> containing all non-SSM template parameters</returns>
        public RuntimeDefinedParameterDictionary GetStackDynamicParameters(IDictionary<string, string> fileParameters)
        {
            var dynamicParams = new RuntimeDefinedParameterDictionaryHelper();

            try
            {
                foreach (var param in this.TemplateParameters)
                {
                    var builder = new RuntimeDefinedParameterBuilder(param.Name, GetClrTypeFromAwsType(param.Type));

                    if (param.Default == null && this.stackOperation == StackOperation.Create && !fileParameters.ContainsKey(param.Name))
                    {
                        // Only make parameter mandatory when creating, and the parameter isn't defined in a parameter file
                        builder.WithMandatory();
                    }

                    if (!string.IsNullOrEmpty(param.Description))
                    {
                        builder.WithHelpMessage(param.Description);
                    }

                    if (param.IsSsmParameter)
                    {
                        // For all AWS::SSM::Parameter types, the value is a string which is the referenced parameter key.
                        // Validation of the actual SSM parameter _value_ is done by CF when the parameter is fetched.
                        builder.WithValidatePattern(new Regex(@"[\w\.\-/]+"));
                        builder.WithValidateLength(1, 2048);
                    }
                    else if (param.Type.Contains("AWS::"))
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
            }
            catch (Exception e)
            {
                this.logger?.LogError($"Error creating dynamic parameters: {e.Message}");
                this.logger?.LogError(e.StackTrace);
                throw;
            }

            return (RuntimeDefinedParameterDictionary)dynamicParams;
        }

        /// <summary>
        /// Gets an AWS parameter type from a string value.
        /// </summary>
        /// <param name="stringValue">The string value.</param>
        /// <returns>The AWS Parameter type</returns>
        public static string GetParameterTypeFromStringValue(string stringValue)
        {
            // Filter out regexes that match strings that are too general
            var filter = new[] { "AWS::EC2::KeyPair::KeyName", "AWS::EC2::SecurityGroup::GroupName" };
            var filteredTypes = AwsParameterTypeRegexes.Where(kv => !filter.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            foreach (var kv in filteredTypes.Where(kv => kv.Value.IsMatch(stringValue)))
            {
                return kv.Key;
            }

            return "String";
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