namespace Firefly.PSCloudFormation.Utils.JsonTraversal
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Basic interface for implementing a JToken visitor, the visitor specifies a context implementation.
    /// One implementation of the context could be used to track the location in the current full Json structure.
    /// <br/>
    /// A visitor context must as minimum implement the <see cref="IJsonVisitorContext{TContext}"/> which provides basic traversal methods.
    /// </summary>
    /// <typeparam name="TContext">Visitor context to use</typeparam>
    internal interface IJsonVisitor<in TContext>
        where TContext : IJsonVisitorContext<TContext>
    {
        void DoAccept(JToken json, TContext context);
    }
}