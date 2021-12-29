namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Text;

    using Firefly.PSCloudFormation.Terraform.State;

    internal class ModuleInputVariable : InputVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleInputVariable"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="values">The value.</param>
        public ModuleInputVariable(string name, params object[] values)
            : base(new DummyParameter(name))
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("Must have at least one value", nameof(values));
            }

            this.CurrentValue = values.Length == 1 ? values[0] : values;
        }

        /// <inheritdoc />
        public override object CurrentValue { get; }

        /// <inheritdoc />
        public override string Type => "string";

        /// <inheritdoc />
        public override string GenerateVariableAssignment()
        {
            if (this.CurrentValue is object[] array)
            {
                var sb = new StringBuilder().AppendLine($"{this.Name} = [");

                foreach (var value in array)
                {
                    sb.AppendLine($"  {RenderObject(value)},");
                }

                sb.AppendLine("]");
                return sb.ToString();
            }

            return $"{this.Name} = {RenderObject(this.CurrentValue)}";
        }

        /// <inheritdoc />
        protected override string GenerateDefaultStanza()
        {
            return string.Empty;
        }

        /// <inheritdoc />
        protected override string GenerateValidationStanza()
        {
            return string.Empty;
        }

        /// <summary>
        /// Renders an object from the values array in the correct format for HCL.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>HCL rendered string representation.</returns>
        private static string RenderObject(object value)
        {
            switch (value)
            {
                case Reference reference:

                    return reference.ReferenceExpression;

                case string s:

                    return $"\"{s}\"";

                case bool b:

                    return b.ToString().ToLowerInvariant();

                default:

                    return value.ToString();
            }
        }
    }
}