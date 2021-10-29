namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal abstract class HclEvent
    {
        /// <summary>
        /// Gets a value indicating the variation of depth caused by this event.
        /// The value can be either -1, 0 or 1. For start events, it will be 1,
        /// for end events, it will be -1, and for the remaining events, it will be 0.
        /// </summary>
        public virtual int NestingIncrease => 0;

        /// <summary>
        /// Gets the event type, which allows for simpler type comparisons.
        /// </summary>
        internal abstract EventType Type { get; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}