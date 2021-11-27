namespace Firefly.PSCloudFormation.Utils.JsonTraversal
{
    /// <summary>
    /// Interface for the a context used by the <see cref="IJsonVisitor{TContext}"/> interface.
    /// <br/>
    /// This interface defines methods for altering the context as the JSON is traversed.
    /// <br/>
    /// It is up to the implementor to choose if that changes a state in the context or if it returns a new context.
    /// </summary>
    /// <typeparam name="TContext">The actual type of the context itself, this is to guide in using the right context type at all times.</typeparam>
    internal interface IJsonVisitorContext<out TContext>
    {
        /// <summary>
        /// Gets the next context for an item in an Array.
        /// </summary>
        /// <param name="index">Index in current JArray</param>
        /// <returns>Current or new context.</returns>
        TContext Next(int index);

        /// <summary>
        /// Gets the next context for a property on an Object.
        /// </summary>
        /// <param name="name">Name of property.</param>
        /// <returns>Current or new context.</returns>
        TContext Next(string name);
    }
}