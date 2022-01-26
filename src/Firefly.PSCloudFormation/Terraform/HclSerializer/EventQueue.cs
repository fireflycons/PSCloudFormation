namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    /// <summary>
    /// Queue implementation used by the emitter.
    /// </summary>
    internal class EventQueue
    {
        /// <summary>
        /// The queue.
        /// </summary>
        private readonly LinkedList<HclEvent> queue = new LinkedList<HclEvent>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueue"/> class.
        /// </summary>
        public EventQueue()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueue"/> class.
        /// </summary>
        /// <param name="events">The events to queue.</param>
        /// <remarks>
        /// Convenience constructor for tests.
        /// </remarks>
        internal EventQueue(IEnumerable<HclEvent> events)
        {
            foreach (var e in events)
            {
                this.Enqueue(e);
            }
        }
        
        /// <summary>
        /// Gets the queue as a collection.
        /// </summary>
        /// <value>
        /// As collection.
        /// </value>
        public IReadOnlyCollection<HclEvent> AsCollection => (IReadOnlyCollection<HclEvent>)this.queue;

        /// <summary>
        /// Gets the count of items in the queue.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => this.queue.Count;

        /// <summary>
        /// Determines whether the queue contains any elements.
        /// </summary>
        /// <returns><c>true</c> if the queue contains elements; otherwise <c>false</c></returns>
        public bool Any()
        {
            return this.queue.Any();
        }

        /// <summary>
        /// Consume this key and its value (scalar or compound).
        /// </summary>
        /// <param name="key">The key to consume.</param>
        /// <returns><c>true</c> if the key was consumed; else <c>false</c> if it was not found.</returns>
        public bool ConsumeKey(MappingKey key)
        {
            var keyNode = this.queue.Find(key);

            if (keyNode == null)
            {
                // This key may already have been consumed because it was inside a schema already consumed earlier.
                return false;
            }

            var valueNode = keyNode.Next;

            if (valueNode == null)
            {
                throw new InvalidOperationException($"End of queue when getting value for attribute \"{key.Path}\".");
            }

            // Remove the key
            this.queue.Remove(keyNode);

            // Remove the value
            this.ConsumeUntil(valueNode, new CompoundAttributeGatherer().Done, true);
            return true;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the queue.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the queue.</returns>
        public HclEvent Dequeue()
        {
            var @event = this.Peek();
            this.queue.RemoveFirst();
            return @event;
        }

        /// <summary>
        /// Adds an object to the end of the queue.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Enqueue(HclEvent @event)
        {
            this.queue.AddLast(@event);
        }

        /// <summary>
        /// Gets a list of all the attribute keys currently held in the queue.
        /// </summary>
        /// <returns>List of all attribute keys.</returns>
        public IEnumerable<MappingKey> GetKeys()
        {
            // Enumerate list now so we get a complete set
            return this.queue.Where(n => n.Type == EventType.MappingKey).Cast<MappingKey>();
        }

        /// <summary>
        /// Returns the object at the beginning of the queue without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the queue.</returns>
        /// <exception cref="System.InvalidOperationException">Queue is empty</exception>
        public HclEvent Peek()
        {
            var @event = this.queue.First;

            if (@event == null)
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return @event.Value;
        }

        /// <summary>
        /// Gets any attributes which conflict with the specified attribute.
        /// </summary>
        /// <param name="key">The attribute to test.</param>
        /// <returns>Enumerable of conflicts.</returns>
        public IEnumerable<MappingKey> GetConflictingAttributes(MappingKey key)
        {
            return this.GetKeys().Where(k => key.Schema.ConflictsWith != null && key.Schema.ConflictsWith.Contains(k.Path));
        }

        /// <summary>
        /// Finds the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns>The event; else <c>null</c> if not found.</returns>
        public LinkedListNode<HclEvent> Find(HclEvent @event)
        {
            return this.queue.Find(@event);
        }

        /// <summary>
        /// Find attribute mapping key by path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The event; else <c>null</c> if not found.</returns>
        public LinkedListNode<HclEvent> FindKeyByPath(string path)
        {
            var tempKey = new MappingKey(
                string.Empty,
                new AttributePath(path),
                ProviderResourceSchema.MissingFromSchemaValueSchema);

            return this.Find(tempKey);
        }

        /// <summary>
        /// Consume items from, and including the front of the queue until a condition is met.
        /// </summary>
        /// <param name="untilFunc">The until function.</param>
        /// <param name="consumeLastItem">If <c>true</c> consume the item that ends the iteration.</param>
        /// <summary></summary>
        public void ConsumeUntil(Func<HclEvent, bool> untilFunc, bool consumeLastItem)
        {
            this.ConsumeUntil(null, untilFunc, consumeLastItem);
        }

        /// <summary>
        /// Consume items from, and including starting node until a condition is met.
        /// </summary>
        /// <param name="startFrom">Node in the queue to begin consumption. If <c>null</c>, start at the front of the queue.</param>
        /// <param name="untilFunc">The until function.</param>
        /// <param name="consumeLastItem">If <c>true</c> consume the item that ends the iteration.</param>
        public void ConsumeUntil(
            LinkedListNode<HclEvent> startFrom,
            Func<HclEvent, bool> untilFunc,
            bool consumeLastItem)
        {
            if (startFrom == null)
            {
                startFrom = this.queue.First;
            }

            var node = startFrom;

            while (node != null)
            {
                if (untilFunc(node.Value))
                {
                    if (consumeLastItem)
                    {
                        this.queue.Remove(node);
                    }

                    return;
                }

                var tempNode = node.Next;
                this.queue.Remove(node);
                node = tempNode;
            }

            throw new InvalidOperationException("Reached end of queue before condition was met.");
        }

        /// <summary>
        /// Peek items starting at front of queue until a condition is met.
        /// </summary>
        /// <param name="untilFunc">The until function.</param>
        /// <param name="emitLastItem">If <c>true</c> emit the item that ends the iteration.</param>
        /// <returns>Enumerable of items peeked.</returns>
        public IEnumerable<HclEvent> PeekUntil(Func<HclEvent, bool> untilFunc, bool emitLastItem)
        {
            return this.PeekUntil(null, untilFunc, emitLastItem);
        }

        /// <summary>
        /// Peek items until a condition is met.
        /// </summary>
        /// <param name="startFrom">Node in the queue to begin peek. If <c>null</c>, start at the front of the queue.</param>
        /// <param name="untilFunc">The until function.</param>
        /// <param name="emitLastItem">If <c>true</c> emit the item that ends the iteration.</param>
        /// <returns>Enumerable of items peeked.</returns>
        public IEnumerable<HclEvent> PeekUntil(LinkedListNode<HclEvent> startFrom, Func<HclEvent, bool> untilFunc, bool emitLastItem)
        {
            if (startFrom == null)
            {
                startFrom = this.queue.First;
            }

            var node = startFrom;

            for (var i = 0; i <= this.Count; ++i)
            {
                if (untilFunc(node.Value))
                {
                    if (emitLastItem)
                    {
                        yield return node.Value;
                    }

                    yield break;
                }

                yield return node.Value;

                node = node.Next;

                if (node == null)
                {
                    throw new InvalidOperationException("Reached end of queue before condition was met.");
                }
            }
        }

        /// <summary>
        /// Class for the debugger to use to display queue items.
        /// </summary>
        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            /// <summary>
            /// The queue
            /// </summary>
            private readonly EventQueue queue;

            /// <summary>
            /// Initializes a new instance of the <see cref="DebugView"/> class.
            /// </summary>
            /// <param name="queue">The queue.</param>
            public DebugView(EventQueue queue)
            {
                this.queue = queue;
            }

            /// <summary>
            /// Gets the items.
            /// </summary>
            /// <value>
            /// The items.
            /// </value>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public HclEvent[] Items
            {
                get
                {
                    var array = new HclEvent[this.queue.Count];
                    this.queue.queue.CopyTo(array, 0);
                    return array;
                }
            }
        }
    }
}