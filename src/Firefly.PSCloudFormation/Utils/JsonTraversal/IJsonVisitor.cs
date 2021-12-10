namespace Firefly.PSCloudFormation.Utils.JsonTraversal
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Basic interface for implementing a JToken visitor. The visitor provides a context implementation.
    /// <br/>
    /// A visitor context must as minimum implement the <see cref="IJsonVisitorContext{TContext}"/> which provides basic traversal methods.
    /// </summary>
    /// <typeparam name="TContext">Visitor context to use</typeparam>
    internal interface IJsonVisitor<in TContext>
        where TContext : IJsonVisitorContext<TContext>
    {
        /// <summary>
        /// Handles acceptance of the visitor
        /// </summary>
        /// <param name="json">The JSON element to visit.</param>
        /// <param name="context">The context.</param>
        void DoAccept(JToken json, TContext context);
    }
}