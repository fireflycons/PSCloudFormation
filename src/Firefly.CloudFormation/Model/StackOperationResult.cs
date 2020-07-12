namespace Firefly.CloudFormation.Model
{
    /// <summary>
    /// Returned by run method of <see cref="CloudFormationRunner"/>
    /// </summary>
    public enum StackOperationResult
    {
        /// <summary>
        /// Stack was unchanged
        /// </summary>
        NoChange,

        /// <summary>
        /// A new stack was created
        /// </summary>
        StackCreated,

        /// <summary>
        /// The stack was updated
        /// </summary>
        StackUpdated,

        /// <summary>
        /// The stack was replaced, i.e. deleted and recreated.
        /// </summary>
        StackReplaced,

        /// <summary>
        /// The stack was deleted.
        /// </summary>
        StackDeleted,

        /// <summary>
        /// Stack creation was initiated, but caller did not want to wait for completion
        /// </summary>
        StackCreateInProgress,

        /// <summary>
        /// Stack update was initiated, but caller did not want to wait for completion
        /// </summary>
        StackUpdateInProgress,

        /// <summary>
        /// Stack delete was initiated, but caller did not want to wait for completion
        /// </summary>
        StackDeleteInProgress
    }
}