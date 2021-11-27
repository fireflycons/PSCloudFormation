namespace Firefly.PSCloudFormation.Utils.JsonTraversal
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Extension methods for JSON.NET to provide visitation.
    /// </summary>
    internal static class JTokenAcceptExtension
    {
        /// <summary>
        /// Performs dispatch for the current token using the visitor's DoAccept method.
        /// </summary>
        /// <returns>This JToken</returns>
        public static T Accept<T, TVisitor>(this T self, TVisitor visitor) 
            where T : JToken where TVisitor : IJsonVisitor<NullJsonVisitorContext>
        {
            visitor.DoAccept(self, new NullJsonVisitorContext());
            return self;
        }

        /// <summary>
        /// Performs dispatch for the current token using the visitor's DoAccept method.
        /// </summary>
        /// <returns>This JToken</returns>
        public static T Accept<T, TContext>(this T self, IJsonVisitor<TContext> visitor, TContext context) 
            where T : JToken where TContext : IJsonVisitorContext<TContext>
        {
            visitor.DoAccept(self, context);
            return self;
        }
    }
}