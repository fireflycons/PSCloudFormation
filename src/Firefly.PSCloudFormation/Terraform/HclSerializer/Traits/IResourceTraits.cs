﻿namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    internal interface IResourceTraits
    {
        /// <summary>
        /// Gets default values for attributes that are <c>null</c> in the state file.
        /// </summary>
        Dictionary<string, object> DefaultValues { get; }

        /// <summary>
        /// Gets the mapping style attributes that are not block definitions
        /// </summary>
        /// <value>
        /// The non block type attributes.
        /// </value>
        List<string> NonBlockTypeAttributes { get; }

        /// <summary>
        /// Gets list of attributes that must be present for the resource in generated HCL.
        /// </summary>
        List<string> RequiredAttributes { get; }

        /// <summary>
        /// Gets the conflicting arguments.
        /// Where arguments conflict. choose the first from this list.
        /// </summary>
        /// <value>
        /// The conflicting arguments.
        /// </value>
        List<string> ConflictingArguments { get; }

        /// <summary>
        /// Gets the unconfigurable attributes.
        /// State properties that have no HCL equivalent, because they are computed.
        /// </summary>
        /// <value>
        /// The unconfigurable attributes.
        /// </value>
        List<string> UnconfigurableAttributes { get; }

        /// <summary>
        /// Applies any default value to the given scalar if its current value is <c>null</c>.
        /// </summary>
        /// <param name="currentPath">Current attribute path.</param>
        /// <param name="scalar">The scalar to check</param>
        /// <returns>Original scalar if unchanged, else new scalar with default value set.</returns>
        Scalar ApplyDefaultValue(string currentPath, Scalar scalar);

        /// <summary>
        /// Determines wither a null or empty attribute should still be emitted.
        /// </summary>
        /// <param name="currentPath">Current attribute path.</param>
        /// <returns><c>true</c> if attribute should be emitted; else <c>false</c></returns>
        bool ShouldEmitAttribute(string currentPath);

        /// <summary>
        /// Determines if argument at current path would conflict with another resource argument, thus should not be emitted
        /// </summary>
        /// <param name="currentPath">The current path.</param>
        /// <returns><c>true</c> if the argument is conflicting; else <c>false</c></returns>
        bool IsConflictingArgument(string currentPath);
    }
}