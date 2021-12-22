namespace Firefly.PSCloudFormation.Terraform.State
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Describes a modification to make to the external state file (i.e. <c>terraform.tfstate</c>).
    /// </summary>
    internal class StateFileModification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateFileModification"/> class.
        /// </summary>
        /// <param name="module">Name of module containing resource.</param>
        /// <param name="resourceName">Name of the resource to modify.</param>
        /// <param name="attributePath">The attribute path in JSON Path syntax.</param>
        /// <param name="newValue">The new value to set.</param>
        public StateFileModification(string module, string resourceName, string attributePath, JToken newValue)
        {
            this.ResourceName = resourceName;
            this.AttributePath = attributePath;
            this.NewValue = newValue;
            this.Module = module;
        }

        /// <summary>
        /// Gets the attribute path.
        /// </summary>
        /// <value>
        /// The attribute path.
        /// </value>
        public string AttributePath { get; }

        /// <summary>
        /// Gets the name of the module containing resource..
        /// </summary>
        /// <value>
        /// The module.
        /// </value>
        public string Module { get; }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        /// <value>
        /// The new value.
        /// </value>
        public JToken NewValue { get; }

        /// <summary>
        /// Gets the name of the resource.
        /// </summary>
        /// <value>
        /// The name of the resource.
        /// </value>
        public string ResourceName { get; }
    }
}