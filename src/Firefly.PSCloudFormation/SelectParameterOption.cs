namespace Firefly.PSCloudFormation
{
    using System.Reflection;

    /// <summary>
    /// Describes what was passed to -Select
    /// </summary>
    internal class SelectParameterOption
    {
        /// <summary>
        /// Gets or sets the selected output.
        /// </summary>
        /// <value>
        /// The selected output.
        /// </value>
        public string SelectedOutput { get; set; }

        /// <summary>
        /// Gets or sets the selected property.
        /// </summary>
        /// <value>
        /// The selected property.
        /// </value>
        public PropertyInfo SelectedProperty { get; set; }
    }
}