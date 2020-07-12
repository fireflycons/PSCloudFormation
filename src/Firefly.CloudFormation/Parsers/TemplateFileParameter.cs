namespace Firefly.CloudFormation.Parsers
{
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents a parameter declaration in a CloudFormation template file
    /// </summary>
    [DebuggerDisplay("{Name}: {Type}")]
    public class TemplateFileParameter
    {
        /// <summary>
        /// Gets or sets Allowed Pattern regex for parameter validation
        /// </summary>
        public Regex AllowedPattern { get; set; }

        /// <summary>
        /// Gets or sets Allowed Values for parameter validation
        /// </summary>
        public string[] AllowedValues { get; set; }

        /// <summary>
        /// Gets or sets a string that explains a constraint when the constraint is violated. 
        /// </summary>
        public string ConstraintDescription { get; set; }

        /// <summary>
        /// Gets or sets parameter default value. When parameter is populated from SSM Parameter Store, this is the parameter path
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Gets or sets the parameter description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has maximum length.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has maximum length; otherwise, <c>false</c>.
        /// </value>
        public bool HasMaxLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has maximum value.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has maximum value; otherwise, <c>false</c>.
        /// </value>
        public bool HasMaxValue { get; set; }

        /// <summary>
        /// Gets a value indicating whether this parameter is populated from SSM Parameter Store
        /// </summary>
        public bool IsSsmParameter => this.Type?.StartsWith("AWS::SSM::Parameter") ?? false;

        /// <summary>
        /// Gets or sets maximum input length. Only valid when type is String
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Gets or sets maximum value. Only valid when type is Number
        /// </summary>
        public double MaxValue { get; set; }

        /// <summary>
        /// Gets or sets minimum input length. Only valid when type is String
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        /// Gets or sets minimum value. Only valid when type is Number
        /// </summary>
        public double MinValue { get; set; }

        /// <summary>
        /// Gets or sets the parameter name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter value should be shown by user interfaces.
        /// </summary>
        public bool NoEcho { get; set; }

        /// <summary>
        /// Gets or sets the parameter type
        /// </summary>
        public string Type { get; set; }
    }
}