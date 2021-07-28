namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using Amazon.CloudFormation.Model;

    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Represents an imported resource
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.ChangeVisualisation.ResourceVertex" />
    internal class ImportedResourceVertex : ResourceVertex
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportedResourceVertex"/> class.
        /// </summary>
        /// <param name="change">The change.</param>
        public ImportedResourceVertex(Change change)
            : base(change)
        {
        }

        /// <summary>
        /// Gets the color of the fill.
        /// </summary>
        /// <value>
        /// The color of the fill.
        /// </value>
        public override GraphvizColor FillColor => GraphvizColor.White;

        /// <summary>
        /// Gets the color of the font.
        /// </summary>
        /// <value>
        /// The color of the font.
        /// </value>
        public override GraphvizColor FontColor => GraphvizColor.Blue;

        /// <summary>
        /// Gets the label.
        /// </summary>
        /// <value>
        /// The label.
        /// </value>
        public override string Label => this.Name;

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>
        /// The style.
        /// </value>
        public override GraphvizVertexStyle Style => GraphvizVertexStyle.Bold;
    }
}