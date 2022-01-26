namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System.Linq;

    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Visits attributes in a state file resource instance, trying to find attribute that matches the one indicated by
    /// the CloudFormation <c>!GetAtt</c>, and getting its value when found.
    /// </summary>
    internal class TerraformAttributeGetterVisitor : JsonVisitor<TerraformAttributeGetterContext>
    {
        /// <inheritdoc />
        protected override void Visit(JProperty property, TerraformAttributeGetterContext context)
        {
            if (context.Success)
            {
                // Already found a value
                return;
            }

            if (!context.AttributeNames.Contains(property.Name))
            {
                // No match
                return;
            }

            // In CloudFormation, the only acceptable types are array or scalar
            // CF does not have object type parameters/variables
            if (property.Value is JArray)
            {
                context.Value = property.Value;
                context.TargetAttributePath = property.Value.Path;
            }

            if (!(property.Value is JValue jv))
            {
                return;
            }

            context.TargetAttributePath = property.Value.Path;

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (jv.Type)
            {
                case JTokenType.String:

                    context.Value = jv.Value<string>();
                    break;

                case JTokenType.Float:

                    context.Value = jv.Value<double>();
                    break;

                case JTokenType.Integer:

                    context.Value = (double)jv.Value<int>();
                    break;

                case JTokenType.Boolean:

                    context.Value = jv.Value<bool>();
                    break;

                case JTokenType.Null:

                    context.Value = null;
                    break;
            }
        }
    }
}