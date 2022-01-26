namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    /// <summary>
    /// Provides a stateful predicate to the PeekUntil and ConsumeUntil methods
    /// to locate the end of a compound attribute.
    /// </summary>
    // ReSharper disable once StyleCop.SA1650
    internal class CompoundAttributeGatherer
    {
        /// <summary>
        /// The current nesting level
        /// </summary>
        private int level;

        /// <summary>
        /// Determines when the end of a nested block is found in the event queue (nesting level returns to zero)
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns><c>true</c> when end of nesting located.</returns>
        public bool Done(HclEvent @event)
        {
            this.level += @event.NestingIncrease;

            return this.level == 0;
        }
    }
}