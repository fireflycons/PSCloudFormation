namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Generic data source input
    /// </summary>
    [DebuggerDisplay("{Address}")]
    internal class DataSourceInput : InputVariable
    {
        /// <summary>
        /// The data source type
        /// </summary>
        private readonly string dataSourceType;

        /// <summary>
        /// The properties
        /// </summary>
        private readonly IDictionary<string, string> arguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceInput"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        public DataSourceInput(string type, string name)
            : this(type, name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceInput"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="arguments">The arguments.</param>
        public DataSourceInput(string type, string name, IDictionary<string, string> arguments)
            : base(new DummyParameter(name))
        {
            this.dataSourceType = type;
            this.arguments = arguments;
        }

        /// <inheritdoc />
        public override string Address => $"{this.dataSourceType}.{this.Name}";

        /// <inheritdoc />
        public override bool IsDataSource => true;

        /// <inheritdoc />
        public override string Type => "string";

        /// <inheritdoc />
        public override string GenerateHcl(bool final)
        {
            var sb = new StringBuilder();

            sb.Append($"data {this.dataSourceType.Quote()} {this.Name.Quote()} ");

            if (this.arguments == null || this.arguments.Count == 0)
            {
                sb.AppendLine("{}");
            }
            else
            {
                sb.AppendLine("{");

                foreach (var property in this.arguments)
                {
                    sb.AppendLine($"  {property.Key} = {property.Value.Quote()}");
                }

                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <inheritdoc />
        public override string GenerateVariableAssignment()
        {
            return string.Empty;
        }

        /// <inheritdoc />
        protected override string GenerateDefaultStanza()
        {
            return string.Empty;
        }

        /// <inheritdoc />
        protected override string GenerateValidationStanza()
        {
            return string.Empty;
        }
    }
}