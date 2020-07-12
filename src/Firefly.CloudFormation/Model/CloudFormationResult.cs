namespace Firefly.CloudFormation.Model
{
    /// <summary>
    /// Result object returned by Create/Update/Delete/Reset methods
    /// </summary>
    public class CloudFormationResult
    {
        /// <summary>
        /// Gets the stack ARN.
        /// </summary>
        /// <value>
        /// The stack ARN.
        /// </value>
        public string StackArn { get; internal set; }

        /// <summary>
        /// Gets the stack operation result.
        /// </summary>
        /// <value>
        /// The stack operation result.
        /// </value>
        public StackOperationResult StackOperationResult { get; internal set; }
    }
}