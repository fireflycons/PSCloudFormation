namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    internal static class EventQueueExtensions
    {
        public static T Consume<T>(this Queue<HclEvent> self)
            where T : HclEvent
        {
            if (self.Count == 0)
            {
                throw new HclSerializerException($"Expected {typeof(T).Name}, but queue was empty.");
            }

            if (!(self.Peek() is T val))
            {
                throw new HclSerializerException($"Expected {typeof(T).Name}, but found {self.Peek().GetType().Name}");
            }

            self.Dequeue();
            return val;
        }
    }
}