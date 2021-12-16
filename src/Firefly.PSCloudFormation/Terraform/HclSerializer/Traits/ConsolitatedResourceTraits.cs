namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Where a given resource type has an entry in <c>ResourceTraits.yaml</c>, an object of this type
    /// is created which is a union of shared traits and resource specific traits
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.IResourceTraits" />
    internal class ConsolitatedResourceTraits : IResourceTraits
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsolitatedResourceTraits"/> class.
        /// </summary>
        /// <param name="sharedTraits">The shared traits.</param>
        /// <param name="specificResourceTraits">The specific resource traits.</param>
        public ConsolitatedResourceTraits(IResourceTraits sharedTraits, IResourceTraits specificResourceTraits)
        {
            this.NonBlockTypeAttributes = sharedTraits.NonBlockTypeAttributes
                .Concat(specificResourceTraits.NonBlockTypeAttributes).ToList();
            this.ComputedAttributes = sharedTraits.ComputedAttributes
                .Concat(specificResourceTraits.ComputedAttributes).ToList();
            this.BlockObjectAttributes = sharedTraits.BlockObjectAttributes
                .Concat(specificResourceTraits.BlockObjectAttributes).ToList();

            // No conflicting arguments defined for all resources
            this.ConflictingArguments = specificResourceTraits.ConflictingArguments;

            // No required attributes defined for all resources
            this.RequiredAttributes = specificResourceTraits.RequiredAttributes;

            // No default values defined for all resources
            this.DefaultValues = specificResourceTraits.DefaultValues;

            // No conditional attributes defined for all resources
            this.ConditionalAttributes = specificResourceTraits.ConditionalAttributes;

            // No attribute map defined for all resources
            this.AttributeMap = specificResourceTraits.AttributeMap;

            this.ResourceType = specificResourceTraits.ResourceType;
        }

        /// <inheritdoc />
        public List<List<string>> ConflictingArguments { get; }

        /// <inheritdoc />
        public List<ConditionalAttribute> ConditionalAttributes { get; }


        /// <inheritdoc />
        public Dictionary<string, string> AttributeMap { get; }

        /// <inheritdoc />
        public Dictionary<string, object> DefaultValues { get; }

        /// <inheritdoc />
        public List<string> NonBlockTypeAttributes { get; }

        /// <inheritdoc />
        public List<string> BlockObjectAttributes { get; set; }

        /// <inheritdoc />
        public List<string> RequiredAttributes { get; }

        /// <inheritdoc />
        public List<string> ComputedAttributes { get; }

        /// <inheritdoc />
        public string ResourceType { get; }

        /// <inheritdoc />
        public Scalar ApplyDefaultValue(string currentPath, Scalar scalar)
        {
            if (this.DefaultValues.Keys.Any(currentPath.IsLike) && scalar.Value == null)
            {
                var newValue = this.DefaultValues[currentPath];
                return new Scalar(newValue, newValue is string);
            }

            return scalar;
        }

        /// <inheritdoc />
        public bool IsConflictingArgument(string currentPath)
        {
            if (!this.ConflictingArguments.Any())
            {
                return false;
            }

            return this.ConflictingArguments.Any(
                argumentGroup => argumentGroup.Any(currentPath.IsLike)
                                 && !currentPath.IsLike(argumentGroup.First()));
        }
    }
}