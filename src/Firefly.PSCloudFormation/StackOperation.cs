namespace Firefly.PSCloudFormation
{
    /// <summary>
    /// Operation to be performed.
    /// Influences how mandatory attribute is applied to dynamic stack parameters with no default
    /// </summary>
    public enum StackOperation
    {
        /// <summary>
        /// Stack is being created. Parameters without defaults must be mandatory
        /// </summary>
        Create,

        /// <summary>
        /// Stack is being updated. Assume parameters without defaults will use previous value.
        /// </summary>
        Update
    }
}