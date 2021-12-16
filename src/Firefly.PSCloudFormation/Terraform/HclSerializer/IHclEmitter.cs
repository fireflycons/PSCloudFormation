namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    internal interface IHclEmitter
    {
        /// <summary>
        /// Emits the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        void Emit(HclEvent @event);
    }
}