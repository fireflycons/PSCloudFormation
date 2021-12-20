namespace Firefly.PSCloudFormation.Terraform.State
{
    /// <summary>
    /// Reference to a data source
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.State.Reference" />
    internal class DataSourceReference : Reference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceReference"/> class.
        /// </summary>
        /// <param name="blockType">Type of the block.</param>
        /// <param name="blockName">Name of the block.</param>
        /// <param name="attribute">The attribute.</param>
        /// <param name="isParameter">If <c>true</c> this represents a parameter, either SSM or pseudo parameter. If <c>false</c> then some other data source.</param>
        public DataSourceReference(string blockType, string blockName, string attribute, bool isParameter)
            : base($"{blockType}.{blockName}.{attribute}")
        {
            this.IsParameter = isParameter;
            this.BlockAddress = $"{blockType}.{blockName}";
            this.Type = blockType;
            this.Name = blockName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceReference"/> class.
        /// </summary>
        /// <param name="objectAddress">The object address.</param>
        /// <remarks>
        /// This constructor used by HCL emitter
        /// </remarks>
        public DataSourceReference(string objectAddress)
            : base(objectAddress)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a parameter.
        /// This is used To determine whether this data source should or should not be added to the inputs list during dependency resolution.
        /// If the data source is a parameter, then it is already present so this boolean saves on a list lookup.
        /// </summary>
        /// <value>
        ///   If <c>true</c> this represents a parameter, either SSM or pseudo parameter. If <c>false</c> then some other data source.
        /// </value>
        public bool IsParameter { get; }

        /// <summary>
        /// Gets the block address.
        /// </summary>
        /// <value>
        /// The block address.
        /// </value>
        public string BlockAddress { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <inheritdoc />
        public override string ReferenceExpression => $"data.{this.ObjectAddress}";
    }
}