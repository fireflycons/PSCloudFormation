namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Utils;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json.Linq;

    /// <content>
    /// This part provides a visitor that finds the attribute targeted by a <c>!GetAtt</c>
    /// and gets the value of that attribute from the state file data which will be used for
    /// value comparisons when generating a <see cref="StateModification"/>.
    /// </content>
    internal partial class ResourceDependencyResolver
    {
        /// <summary>
        /// Context for <see cref="TerraformAttributeGetterVisitor"/>
        /// </summary>
        private class TerraformAttributeGetterContext : IJsonVisitorContext<TerraformAttributeGetterContext>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TerraformAttributeGetterContext"/> class.
            /// </summary>
            /// <param name="attributeName">Name of the resource attribute to search for.</param>
            public TerraformAttributeGetterContext(string attributeName)
            {
                // Attribute in TF resource may have the same name as CF, but more likely a snake case version.
                this.AttributeNames = new[] { attributeName.CamelCaseToSnakeCase(), attributeName };
            }

            /// <summary>
            /// Gets the names of the attribute - snake case and unmodified.
            /// </summary>
            /// <value>
            /// The name of the attribute.
            /// </value>
            public IEnumerable<string> AttributeNames { get; }

            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            /// <value>
            /// The value associated with the attribute, or <c>null</c> if attribute not found.
            /// </value>
            public object Value { get; set; }

            /// <summary>
            /// Gets or sets the attribute reference.
            /// </summary>
            /// <value>
            /// Reference to the located attribute if found, else <c>null</c>.
            /// </value>
            public JProperty AttributeReference { get; set; }

            /// <summary>
            /// Gets a value indicating whether the attribute value is scalar.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is scalar; otherwise, <c>false</c>.
            /// </value>
            public bool IsScalar => this.Value == null || this.Value is string || !(this.Value is IEnumerable);

            /// <inheritdoc />
            public TerraformAttributeGetterContext Next(int index)
            {
                return this;
            }

            /// <inheritdoc />
            public TerraformAttributeGetterContext Next(string name)
            {
                return this;
            }
        }

        /// <summary>
        /// Visits attributes in a state file resource instance, trying to find attribute that matches the one indicated by
        /// the CloudFormation <c>!GetAtt</c>, and getting its value when found.
        /// </summary>
        private class TerraformAttributeGetterVisitor : JsonVisitor<TerraformAttributeGetterContext>
        {
            /// <inheritdoc />
            protected override void Visit(JProperty json, TerraformAttributeGetterContext context)
            {
                if (context.Value != null)
                {
                    // Already found a value
                    return;
                }

                if (!context.AttributeNames.Contains(json.Name))
                {
                    // No match
                    return;
                }

                if (json.Value is JArray)
                {
                    context.Value = json.Value;
                }

                if (!(json.Value is JValue jv))
                {
                    return;
                }

                context.AttributeReference = json;

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
                }
            }
        }
    }
}