namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

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
            this.UnconfigurableAttributes = sharedTraits.UnconfigurableAttributes
                .Concat(specificResourceTraits.UnconfigurableAttributes).ToList();

            // No conflicting arguments defined for all resources
            this.ConflictingArguments = specificResourceTraits.ConflictingArguments;

            // No required attributes defined for all resources
            this.RequiredAttributes = specificResourceTraits.RequiredAttributes;

            // No default values defined for all resources
            this.DefaultValues = specificResourceTraits.DefaultValues;
        }

        /// <inheritdoc />
        public List<string> ConflictingArguments { get; }

        /// <inheritdoc />
        public Dictionary<string, object> DefaultValues { get; }

        /// <inheritdoc />
        public List<string> NonBlockTypeAttributes { get; }

        /// <inheritdoc />
        public List<string> RequiredAttributes { get; }

        /// <inheritdoc />
        public List<string> UnconfigurableAttributes { get; }

        /// <inheritdoc />
        public Scalar ApplyDefaultValue(string currentPath, Scalar scalar)
        {
            if (this.DefaultValues.ContainsKey(currentPath) && scalar.Value == null)
            {
                var newValue = this.DefaultValues[currentPath];
                return new Scalar(newValue, newValue is string);
            }

            return scalar;
        }

        /// <inheritdoc />
        public bool ShouldEmitAttribute(string currentPath)
        {
            if (this.ConflictingArguments.Any() && this.ConflictingArguments.Contains(currentPath)
                                                && this.ConflictingArguments.First() != currentPath)
            {
                return false;
            }


            return this.RequiredAttributes.Contains(currentPath) || this.DefaultValues.ContainsKey(currentPath);
        }

        /// <inheritdoc />
        public bool IsConflictingArgument(string currentPath)
        {
            if (!this.ConflictingArguments.Any() || !this.ConflictingArguments.Contains(currentPath))
            {
                return false;
            }

            return this.ConflictingArguments.First() != currentPath;
        }
    }
}