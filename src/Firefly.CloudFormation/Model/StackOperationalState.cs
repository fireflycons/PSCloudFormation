namespace Firefly.CloudFormation.Model
{
    /// <summary>
    /// Values that describe readiness state in order to receive updates
    /// </summary>
    public enum StackOperationalState
    {
        /// <summary>
        /// The stack doesn't exist, i.e. can be created.
        /// </summary>
        NotFound,

        /// <summary>
        /// Stack is ready, i.e. can be updated or deleted.
        /// </summary>
        Ready,

        /// <summary>
        /// Stack is busy, i.e a create or update operation is in progress
        /// </summary>
        Busy,

        /// <summary>
        /// A delete operation is in progress
        /// </summary>
        Deleting,

        /// <summary>
        /// A previous delete failed, however in this state delete may be retried.
        /// </summary>
        DeleteFailed,

        /// <summary>
        /// Stack is broken - e.g. a rollback failed
        /// </summary>
        Broken
    }
}