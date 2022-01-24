namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Where a given resource type has an entry in <c>ResourceTraits.yaml</c>, an object of this type
    /// is created which is a union of shared traits and resource specific traits
    /// </summary>
    /// <seealso cref="IResourceTraits" />
    internal class ConsolitatedResourceTraits : IResourceTraits
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsolitatedResourceTraits"/> class.
        /// </summary>
        /// <param name="sharedTraits">The shared traits.</param>
        /// <param name="specificResourceTraits">The specific resource traits.</param>
        // ReSharper disable once UnusedParameter.Local - It might be one day
        public ConsolitatedResourceTraits(IResourceTraits sharedTraits, IResourceTraits specificResourceTraits)
        {
            // No conflicting arguments defined for all resources
            this.ConflictingArguments = specificResourceTraits.ConflictingArguments;

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