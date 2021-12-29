namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.Utils;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    /// <summary>
    /// Context for <see cref="TerraformAttributeGetterVisitor"/>
    /// </summary>
    internal class TerraformAttributeGetterContext : IJsonVisitorContext<TerraformAttributeGetterContext>, IGetAttTargetEvaluation
    {
        /// <summary>
        /// Underlying value within state.
        /// </summary>
        private object value;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerraformAttributeGetterContext"/> class.
        /// </summary>
        /// <param name="attributeName">Name of the resource attribute to search for.</param>
        public TerraformAttributeGetterContext(string attributeName)
        {
            // Attribute in TF resource may have the same name as CF, but more likely a snake case version.
            this.AttributeNames = new[] { attributeName.CamelCaseToSnakeCase(), attributeName };
        }

        /// <summary>
        /// Gets the names of the attribute - snake case and unmodified.
        /// </summary>
        /// <value>
        /// The name of the attribute.
        /// </value>
        public IEnumerable<string> AttributeNames { get; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value associated with the attribute, or <c>null</c> if attribute not found.
        /// </value>
        public object Value
        {
            get => this.value;

            set
            {
                this.value = value;
                this.Success = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the target of the <c>!GetAtt</c> was successfully located..
        /// </summary>
        /// <value>
        ///   <c>true</c> if success; otherwise, <c>false</c>.
        /// </value>
        public bool Success { get; private set; }

        /// <summary>
        /// Gets or sets the JSON path of the attribute where the value was located.
        /// </summary>
        /// <value>
        /// The JSON path of the target attribute.
        /// </value>
        public string TargetAttributePath { get; set; }

        /// <inheritdoc />
        public TerraformAttributeGetterContext Next(int index)
        {
            return this;
        }

        /// <inheritdoc />
        public TerraformAttributeGetterContext Next(string name)
        {
            return this;
        }
    }
}