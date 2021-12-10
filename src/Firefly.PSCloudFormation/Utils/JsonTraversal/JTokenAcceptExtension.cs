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
        /// <typeparam name="T">Type of object being visited</typeparam>
        /// <typeparam name="TVisitor">The type of the visitor.</typeparam>
        /// <param name="self">The self.</param>
        /// <param name="visitor">The visitor.</param>
        /// <returns>This JToken</returns>
        public static T Accept<T, TVisitor>(this T self, TVisitor visitor)
            where T : JToken where TVisitor : IJsonVisitor<NullJsonVisitorContext>
        {
            visitor.DoAccept(self, new NullJsonVisitorContext());
            return self;
        }

        /// <summary>
        /// Accepts the specified visitor.
        /// </summary>
        /// <typeparam name="T">Type of object being visited</typeparam>
        /// <typeparam name="TContext">The type of the context.</typeparam>
        /// <param name="self">The self.</param>
        /// <param name="visitor">The visitor.</param>
        /// <param name="context">The context.</param>
        /// <returns>This JToken</returns>
        public static T Accept<T, TContext>(this T self, IJsonVisitor<TContext> visitor, TContext context)
            where T : JToken where TContext : IJsonVisitorContext<TContext>
        {
            visitor.DoAccept(self, context);
            return self;
        }
    }
}