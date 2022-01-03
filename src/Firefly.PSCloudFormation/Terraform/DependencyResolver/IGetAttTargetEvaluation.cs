namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    /// <summary>
    /// The return type of a state resource visit to locate the target of a <c>!GetAtt</c>
    /// </summary>
    internal interface IGetAttTargetEvaluation
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value associated with the attribute.
        /// </value>
        object Value { get; }

        /// <summary>
        /// Gets a value indicating whether the target of the <c>!GetAtt</c> was successfully located..
        /// </summary>
        /// <value>
        ///   <c>true</c> if success; otherwise, <c>false</c>.
        /// </value>
        bool Success { get; }

        /// <summary>
        /// Gets the JSON path of the attribute where the value was located.
        /// </summary>
        /// <value>
        /// The JSON path of the target attribute.
        /// </value>
        string TargetAttributePath { get; }
    }
}