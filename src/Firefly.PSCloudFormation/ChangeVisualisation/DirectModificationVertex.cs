namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Diagnostics;

    using QuikGraph.Graphviz.Dot;

    [DebuggerDisplay("{Name}")]
    internal class DirectModificationVertex : IChangeVertex
    {
        public string Name => "Direct Modification";

        public GraphvizVertexShape Shape => GraphvizVertexShape.Ellipse;

        public override string ToString()
        {
            return this.Name;
        }
    }
}