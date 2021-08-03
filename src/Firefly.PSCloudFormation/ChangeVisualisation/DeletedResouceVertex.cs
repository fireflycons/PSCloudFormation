namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Diagnostics;

    using Amazon.CloudFormation.Model;

    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Represents a deleted resource
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.ChangeVisualisation.ResourceVertex" />
    [DebuggerDisplay("Delete: {Name}")]
    internal class DeletedResouceVertex : ResourceVertex
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeletedResouceVertex"/> class.
        /// </summary>
        /// <param name="change">The change.</param>
        public DeletedResouceVertex(Change change)
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
        public override GraphvizColor FontColor => GraphvizColor.Red;

        /// <summary>
        /// Gets the label.
        /// </summary>
        /// <value>
        /// The label.
        /// </value>
        public override string Label => $"<S>{this.Name}</S>";

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>
        /// The style.
        /// </value>
        public override GraphvizVertexStyle Style => GraphvizVertexStyle.Dashed;
    }
}