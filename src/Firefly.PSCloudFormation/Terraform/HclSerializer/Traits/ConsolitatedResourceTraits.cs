﻿namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Utils;

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

            this.ResourceType = specificResourceTraits.ResourceType;
        }

        /// <inheritdoc />
        public List<List<string>> ConflictingArguments { get; }

        /// <inheritdoc />
        public Dictionary<string, object> DefaultValues { get; }

        /// <inheritdoc />
        public List<string> NonBlockTypeAttributes { get; }

        /// <inheritdoc />
        public List<string> RequiredAttributes { get; }

        /// <inheritdoc />
        public List<string> UnconfigurableAttributes { get; }

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