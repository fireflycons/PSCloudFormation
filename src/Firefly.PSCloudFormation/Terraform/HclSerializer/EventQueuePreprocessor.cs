namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    /// <summary>
    /// Provides methods for preprocessing the event queue prior to serialization.
    /// Removes all attributes that should not be serialized because they are either
    /// - Optional and are null or have the default value.
    /// - Computed and not optional or required.
    /// - In conflict with another attribute with higher priority.
    /// </summary>
    internal class EventQueuePreprocessor
    {
        /// <summary>
        /// Analysis results that describe an attribute having no value (null, default for scalar type or empty collection or block.
        /// </summary>
        private static readonly AttributeContent[] EmptyAttributeAnalysisResults =
            {
                AttributeContent.Null, AttributeContent.Empty, AttributeContent.EmptyCollection
            };

        /// <summary>
        /// The queue
        /// </summary>
        private readonly EventQueue queue;

        /// <summary>
        /// The traits
        /// </summary>
        private readonly IResourceTraits traits;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueuePreprocessor"/> class.
        /// </summary>
        /// <param name="queue">The event queue.</param>
        public EventQueuePreprocessor(EventQueue queue)
        {
            this.queue = queue;
            this.traits = AwsSchema.GetResourceTraits(this.GetResourceType());
        }

        /// <summary>
        /// Performs the 3 preprocessing steps
        /// </summary>
        public void ProcessQueue()
        {
            this.RemoveEmptyOptionalAttributes();
            this.RemoveComputedAttributes();
            this.ResolveConflicts();
        }

        /// <summary>
        /// Resolve conflicts in the event queue, eliminating keys that should not be emitted.
        /// </summary>
        /// <exception cref="Firefly.PSCloudFormation.Terraform.HclSerializer.ConflictResolutionException">
        /// Unable to resolve conflict between \"{argument1}\" and \"{argument2}\". Please raise an issue.
        /// </exception>
        internal void ResolveConflicts()
        {
            // Enumerate all attribute keys with conflicting keys indicated in their schema
            foreach (var key in this.queue.GetKeys().Where(k => k.Schema.ConflictsWith.Any()).ToList())
            {
                var analysis = this.AnalyzeAttribute(key);

                if (analysis == AttributeContent.NotFound)
                {
                    // Already eliminated
                    continue;
                }

                // If this value is optional and empty, eliminate it
                if (key.Schema.Optional && IsEmptyValue(analysis))
                {
                    this.queue.ConsumeKey(key);
                    continue;
                }

                // Now check all the conflicting attributes, and eliminate any that are empty
                foreach (var conflictingKey in (from conflictingKeyPath in key.Schema.ConflictsWith
                                                select this.queue.FindKeyByPath(conflictingKeyPath)
                                                into node
                                                where node != null
                                                select node.Value).OfType<MappingKey>())
                {
                    if (conflictingKey.Schema.Optional && IsEmptyValue(this.AnalyzeAttribute(conflictingKey)))
                    {
                        this.queue.ConsumeKey(conflictingKey);
                        continue;
                    }

                    // If we get here, then try to resolve with traits
                    var conflictGroup = this.traits.ConflictingArguments.FirstOrDefault(
                        g => g.Contains(key.Path) && g.Contains(conflictingKey.Path));

                    if (conflictGroup == null)
                    {
                        // Need to update ResourceTraits.yaml
                        throw new ConflictResolutionException(key.Path, conflictingKey.Path);
                    }

                    // Highest in the conflict group wins, and the loser is eliminated
                    var loser = conflictGroup[Math.Max(
                        conflictGroup.IndexOf(key.Path),
                        conflictGroup.IndexOf(conflictingKey.Path))];
                    this.queue.ConsumeKey(new[] { key, conflictingKey }.First(k => k.Path == loser));
                }
            }
        }

        /// <summary>
        /// Remove any attributes that are optional and have no defined value
        /// </summary>
        internal void RemoveEmptyOptionalAttributes()
        {
            // TODO: also process ConditionalAttributes from traits
            foreach (var key in from key in this.queue.GetKeys().Where(k => k.Schema.Optional).ToList()
                                let analysis = this.AnalyzeAttribute(key)
                                where analysis != AttributeContent.NotFound
                                where IsEmptyValue(analysis)
                                select key)
            {
                this.queue.ConsumeKey(key);
            }
        }

        /// <summary>
        /// Preprocess the event queue scanning for attributes that are defined as computed only in the schema.
        /// Such attributes can never appear in HCL, so we can eliminate them.
        /// The schema definition of such keys is Computed = true; Optional = false; Required = false
        /// </summary>
        internal void RemoveComputedAttributes()
        {
            foreach (var key in this.queue.GetKeys().Where(k => k.Schema.IsComputedOnly).ToList())
            {
                this.queue.ConsumeKey(key);
            }

            // Special case in that tags_all is computed/optional, but we still want to remove it.
            var tagsAll = this.queue.Find(new MappingKey("tags_all", new AttributePath("tags_all"), ProviderResourceSchema.MissingFromSchemaValueSchema));

            if (tagsAll != null)
            {
                this.queue.ConsumeKey((MappingKey)tagsAll.Value);
            }
        }

        /// <summary>
        /// Determines whether the specified analysis represents an empty value, collection or block
        /// </summary>
        /// <param name="analysis">The analysis.</param>
        /// <returns>
        ///   <c>true</c> if [is empty value] [the specified analysis]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsEmptyValue(AttributeContent analysis)
        {
            return EmptyAttributeAnalysisResults.Contains(analysis);
        }

        /// <summary>
        /// Analyzes an attribute's value to see whether it has a value, is null or is an empty collection.
        /// </summary>
        /// <param name="key">The attribute key.</param>
        /// <returns>Result of analysis.</returns>
        /// <exception cref="Firefly.PSCloudFormation.Terraform.HclSerializer.HclSerializerException">Expected MappingStart, SequenceStart or PolicyStart. Got {nextEvent.GetType().Name}</exception>
        private AttributeContent AnalyzeAttribute(MappingKey key)
        {
            var keyNode = this.queue.Find(key);

            if (keyNode == null)
            {
                return AttributeContent.NotFound;
            }

            var nextEventNode = keyNode.Next;

            if (nextEventNode == null)
            {
                throw new InvalidOperationException($"Unexpected end of event queue after attribute {key.Path}");
            }

            var nextEvent = nextEventNode.Value;

            var currentAnalysis = key.InitialAnalysis;

            switch (nextEvent)
            {
                case Scalar scalar:

                    return scalar.Analyze(key, this.traits);

                case JsonStart _:

                    return AttributeContent.Value;
            }

            if (!(nextEvent is CollectionStart))
            {
                throw new InvalidOperationException(
                    $"Expected MappingStart, SequenceStart or JsonStart. Got {nextEvent.GetType().Name}");
            }

            // Read ahead the entire collection
            var collection = this.queue.PeekUntil(nextEventNode, new CompoundAttributeGatherer().Done, true).ToList();

            return collection.Any(e => e is ScalarValue sv && !sv.IsEmpty)
                       ? currentAnalysis
                       : AttributeContent.EmptyCollection;
        }

        /// <summary>
        /// Gets the type of the resource from the ResourceStart event in the queue.
        /// </summary>
        /// <returns>Terraform resource type.</returns>
        /// <exception cref="System.InvalidOperationException">Expected ResourceStart but could not find it. Event queue is in invalid state.</exception>
        private string GetResourceType()
        {
            var resourceKey = this.queue.AsCollection.FirstOrDefault(k => k is ResourceStart);

            if (resourceKey == null)
            {
                throw new InvalidOperationException(
                    "Expected ResourceStart but could not find it. Event queue is in invalid state.");
            }

            return ((ResourceStart)resourceKey).ResourceType;
        }
    }
}