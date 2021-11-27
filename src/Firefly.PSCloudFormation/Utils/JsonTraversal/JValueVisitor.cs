namespace Firefly.PSCloudFormation.Utils.JsonTraversal
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Adds a range of extra visited methods for JValues such as Integer, Float, String, Boolean etc.
    /// 
    /// Default implementation for all specific times routes back to <see cref="JsonVisitor`1{T}.Visit(JToken,TContext)"/>
    /// </summary>
    /// <typeparam name="TContext">Type of the context object associated with the visit</typeparam>
    internal abstract class JValueVisitor<TContext> : JsonVisitor<TContext>
        where TContext : IJsonVisitorContext<TContext>
    {
        protected override void Visit(JRaw json, TContext context)
        {
            this.VisitRaw(json, context);
        }

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

        protected virtual void VisitBoolean(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitBytes(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitComment(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitDate(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitFloat(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitGuid(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitInteger(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitNull(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitRaw(JRaw json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitString(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitTimeSpan(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitUndefined(JValue json, TContext context) => this.Visit((JToken)json, context);

        protected virtual void VisitUri(JValue json, TContext context) => this.Visit((JToken)json, context);
    }
}