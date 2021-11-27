namespace Firefly.PSCloudFormation.Utils.JsonTraversal
{
    using System;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A JSON.NET JToken Visitor class that traverses a JSON object and calls the appropriate visit methods for all nodes in
    /// the object.
    /// 
    /// This is not a true visitor approach since that would require changes in JSON.NET, instead it bases it's dispatch
    /// of the <see cref="JToken.Type"/>.
    /// 
    /// DoAccept performs the dispatch and any type of dispatch can be implemented by overriding this method.
    /// 
    /// The visitor accepts a Context object which can be used as an alternative to tracking during the visiting process.
    /// </summary>
    /// <typeparam name="TContext">Type of the context object associated with the visit</typeparam>
    internal abstract class JsonVisitor<TContext> : IJsonVisitor<TContext>
        where TContext : IJsonVisitorContext<TContext>
    {
        // JToken
        // ├─ JContainer
        // │   ├─ JArray
        // │   ├─ JConstructor
        // │   ├─ JObject
        // │   └─ JProperty
        // └─ JValue
        // └─ JRaw

        /// <summary>
        /// Handles acceptance if the visitor
        /// </summary>
        /// <param name="json">The JSON element to visit.</param>
        /// <param name="context">The context.</param>
        /// <exception cref="System.InvalidOperationException">Invalid JToken type '{json.Type}'.</exception>
        public virtual void DoAccept(JToken json, TContext context)
        {
            switch (json.Type)
            {
                case JTokenType.Object:
                    this.Visit((JObject)json, context);
                    break;

                case JTokenType.Array:
                    this.Visit((JArray)json, context);
                    break;

                case JTokenType.Raw:
                    this.Visit((JRaw)json, context);
                    break;

                case JTokenType.Constructor:
                    this.Visit((JConstructor)json, context);
                    break;

                case JTokenType.Property:
                    this.Visit((JProperty)json, context);
                    break;

                case JTokenType.Comment:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                    this.Visit((JValue)json, context);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid JToken type '{json.Type}'.");
            }
        }

        /// <summary>
        /// Visits the specified token.
        /// </summary>
        /// <param name="json">The token.</param>
        /// <param name="context">The context.</param>
        protected virtual void Visit(JToken json, TContext context)
        {
        }

        /// <summary>
        /// Visits the specified container.
        /// </summary>
        /// <param name="json">The container.</param>
        /// <param name="context">The context.</param>
        protected virtual void Visit(JContainer json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits the specified array.
        /// </summary>
        /// <param name="json">The array.</param>
        /// <param name="context">The context.</param>
        protected virtual void Visit(JArray json, TContext context)
        {
            int i = 0;
            foreach (JToken token in json)
            {
                token.Accept(this, context.Next(i++));
            }
        }

        /// <summary>
        /// Visits the specified constructor.
        /// </summary>
        /// <param name="json">The constructor.</param>
        /// <param name="context">The context.</param>
        protected virtual void Visit(JConstructor json, TContext context) => this.Visit((JContainer)json, context);

        /// <summary>
        /// Visits the specified object.
        /// </summary>
        /// <param name="json">The object.</param>
        /// <param name="context">The context.</param>
        protected virtual void Visit(JObject json, TContext context)
        {
            foreach (JProperty property in json.Properties())
            {
                this.Visit(property, context);
            }
        }

        /// <summary>
        /// Visits the specified property.
        /// </summary>
        /// <param name="json">The property.</param>
        /// <param name="context">The context.</param>
        protected virtual void Visit(JProperty json, TContext context)
        {
            json.Value.Accept(this, context.Next(json.Name));
        }

        /// <summary>
        /// Visits the specified value.
        /// </summary>
        /// <param name="json">The value.</param>
        /// <param name="context">The context.</param>
        protected virtual void Visit(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits the specified raw value.
        /// </summary>
        /// <param name="json">The raw value.</param>
        /// <param name="context">The context.</param>
        protected virtual void Visit(JRaw json, TContext context) => this.Visit((JValue)json, context);
    }
}