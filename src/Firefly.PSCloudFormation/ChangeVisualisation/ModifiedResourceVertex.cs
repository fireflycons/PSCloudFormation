namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Diagnostics;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Represents a modified resource
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.ChangeVisualisation.ResourceVertex" />
    [DebuggerDisplay("Modify: {Name}")]
    internal class ModifiedResourceVertex : ResourceVertex
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModifiedResourceVertex"/> class.
        /// </summary>
        /// <param name="change">The change.</param>
        public ModifiedResourceVertex(Change change)
            : base(change)
        {
        }

        /// <summary>
        /// Gets the color of the fill.
        /// </summary>
        /// <value>
        /// The color of the fill.
        /// </value>
        public override GraphvizColor FillColor =>
            this.Change.ResourceChange.Replacement == Replacement.True ? GraphvizColor.OrangeRed :
            this.Change.ResourceChange.Replacement == Replacement.Conditional ? GraphvizColor.Orange :
            GraphvizColor.LightGreen;

        /// <summary>
        /// Gets the color of the font.
        /// </summary>
        /// <value>
        /// The color of the font.
        /// </value>
        public override GraphvizColor FontColor => GraphvizColor.Black;

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
        public override GraphvizVertexStyle Style => GraphvizVertexStyle.Filled;
    }
}