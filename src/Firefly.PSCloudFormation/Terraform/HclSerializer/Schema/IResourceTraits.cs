namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
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
        /// Gets the conflicting arguments.
        /// Where arguments conflict. choose the first from this list.
        /// </summary>
        /// <value>
        /// The conflicting arguments.
        /// </value>
        List<List<string>> ConflictingArguments { get; }

        /// <summary>
        /// Gets the list of conditional attributes.
        /// </summary>
        /// <value>
        /// The conditional attributes.
        /// </value>
        List<ConditionalAttribute> ConditionalAttributes { get; }

        /// <summary>
        /// Gets the map of CloudFormation attribute name to terraform attribute name
        /// </summary>
        /// <value>
        /// The attribute map.
        /// </value>
        Dictionary<string, string> AttributeMap { get; }

        /// <summary>
        /// Gets the terraform resource type.
        /// </summary>
        /// <value>
        /// The type of the resource.
        /// </value>
        string ResourceType { get; }

        /// <summary>
        /// Applies any default value to the given scalar if its current value is <c>null</c>.
        /// </summary>
        /// <param name="currentPath">Current attribute path.</param>
        /// <param name="scalar">The scalar to check</param>
        /// <returns>Original scalar if unchanged, else new scalar with default value set.</returns>
        Scalar ApplyDefaultValue(string currentPath, Scalar scalar);

        /// <summary>
        /// Determines if argument at current path would conflict with another resource argument, thus should not be emitted
        /// </summary>
        /// <param name="currentPath">The current path.</param>
        /// <returns><c>true</c> if the argument is conflicting; else <c>false</c></returns>
        bool IsConflictingArgument(string currentPath);
    }
}