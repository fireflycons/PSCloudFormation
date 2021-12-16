namespace Firefly.PSCloudFormation.Utils.JsonTraversal
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Adds a range of extra visited methods for JValues such as Integer, Float, String, Boolean etc.
    /// 
    /// Default implementation for all specific types routes back to <see cref="JsonVisitor{TContext}"/> 
    /// </summary>
    /// <typeparam name="TContext">Type of the context object associated with the visit</typeparam>
    internal abstract class JValueVisitor<TContext> : JsonVisitor<TContext>
        where TContext : IJsonVisitorContext<TContext>
    {
        /// <summary>
        /// Visits the specified raw value.
        /// </summary>
        /// <param name="json">The raw value.</param>
        /// <param name="context">The context.</param>
        protected override void Visit(JRaw json, TContext context)
        {
            this.VisitRaw(json, context);
        }

        /// <summary>
        /// Visits the specified value.
        /// </summary>
        /// <param name="json">The value.</param>
        /// <param name="context">The context.</param>
        protected override void Visit(JValue json, TContext context)
        {
            switch (json.Type)
            {
                case JTokenType.Comment:
                    this.VisitComment(json, context);
                    break;
                case JTokenType.Integer:
                    this.VisitInteger(json, context);
                    break;
                case JTokenType.Float:
                    this.VisitFloat(json, context);
                    break;
                case JTokenType.String:
                    this.VisitString(json, context);
                    break;
                case JTokenType.Boolean:
                    this.VisitBoolean(json, context);
                    break;
                case JTokenType.Null:
                    this.VisitNull(json, context);
                    break;
                case JTokenType.Undefined:
                    this.VisitUndefined(json, context);
                    break;
                case JTokenType.Date:
                    this.VisitDate(json, context);
                    break;
                case JTokenType.Bytes:
                    this.VisitBytes(json, context);
                    break;
                case JTokenType.Guid:
                    this.VisitGuid(json, context);
                    break;
                case JTokenType.Uri:
                    this.VisitUri(json, context);
                    break;
                case JTokenType.TimeSpan:
                    this.VisitTimeSpan(json, context);
                    break;
            }
        }

        /// <summary>
        /// Visits a boolean.
        /// </summary>
        /// <param name="json">The boolean value.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitBoolean(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a bytes array.
        /// </summary>
        /// <param name="json">The byte array.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitBytes(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a comment.
        /// </summary>
        /// <param name="json">The comment.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitComment(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a date.
        /// </summary>
        /// <param name="json">The date.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitDate(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a float.
        /// </summary>
        /// <param name="json">The float.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitFloat(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a unique identifier.
        /// </summary>
        /// <param name="json">The GUID.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitGuid(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits an integer.
        /// </summary>
        /// <param name="json">The integer.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitInteger(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a null.
        /// </summary>
        /// <param name="json">The null.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitNull(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a raw value.
        /// </summary>
        /// <param name="json">The raw value.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitRaw(JRaw json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a string.
        /// </summary>
        /// <param name="json">The string.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitString(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a time span.
        /// </summary>
        /// <param name="json">The time span.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitTimeSpan(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits an undefined value.
        /// </summary>
        /// <param name="json">The undefined value.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitUndefined(JValue json, TContext context) => this.Visit((JToken)json, context);

        /// <summary>
        /// Visits a URI.
        /// </summary>
        /// <param name="json">The URI.</param>
        /// <param name="context">The context.</param>
        protected virtual void VisitUri(JValue json, TContext context) => this.Visit((JToken)json, context);
    }
}