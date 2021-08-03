namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Diagnostics;

    using Amazon.CloudFormation.Model;

    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Represents a new resource
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.ChangeVisualisation.ResourceVertex" />
    [DebuggerDisplay("Add: {Name}")]
    internal class NewResourceVertex : ResourceVertex
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewResourceVertex"/> class.
        /// </summary>
        /// <param name="change">The change.</param>
        public NewResourceVertex(Change change)
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
        public override GraphvizColor FontColor => GraphvizColor.Green;

        /// <summary>
        /// Gets the label.
        /// </summary>
        /// <value>
        /// The label.
        /// </value>
        public override string Label => $"<B>{this.Name}</B>";

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>
        /// The style.
        /// </value>
        public override GraphvizVertexStyle Style => GraphvizVertexStyle.Bold;
    }
}