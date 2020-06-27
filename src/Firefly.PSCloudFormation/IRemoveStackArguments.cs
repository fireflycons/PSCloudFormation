namespace Firefly.PSCloudFormation
{
    /// <summary>
    /// Parameters related to stack removal.
    /// </summary>
    public interface IRemoveStackArguments
    {
        /// <summary>
        /// Gets or sets the retain resource.
        /// <para type="description">
        /// For stacks in the <c>DELETE_FAILED</c> state, a list of resource logical IDs that are associated with the resources you want to retain.
        /// During deletion, AWS CloudFormation deletes the stack but does not delete the retained resources.
        /// Retaining resources is useful when you cannot delete a resource, such as a non-empty S3 bucket, but you want to delete the stack.
        /// </para>
        /// </summary>
        /// <value>
        /// The retain resource.
        /// </value>
        string[] RetainResource { get; set; }
    }
}