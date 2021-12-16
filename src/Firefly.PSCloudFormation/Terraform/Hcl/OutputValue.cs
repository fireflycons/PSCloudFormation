namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Text;

    internal class OutputValue
    {
        private readonly string name;

        private readonly string reference;

        private readonly string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputValue"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="reference">The reference.</param>
        public OutputValue(string name, string reference)
        {
            this.reference = reference;
            this.name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputValue"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="reference">The reference.</param>
        /// <param name="description">The description.</param>
        public OutputValue(string name, string reference, string description)
        : this(name, reference)
        {
            this.description = description;
        }

        /// <summary>
        /// Generates HCL code for this output.
        /// </summary>
        /// <returns>HCL code for the output.</returns>
        public string GenerateHcl()
        {
            var hcl = new StringBuilder();

            hcl.AppendLine($"output \"{this.name}\" {{");
            hcl.AppendLine($"  value = {this.reference}");

            if (this.description != null)
            {
                hcl.AppendLine($"  description = \"{this.description}\"");
            }

            hcl.AppendLine("}");

            return hcl.ToString();
        }
    }
}