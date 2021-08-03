namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Interface describing a vertex in a DOT graph
    /// </summary>
    internal interface IChangeVertex
    {
        /// <summary>
        /// Gets the name of the object represented by the vertex.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets the shape with which to draw the vertex.
        /// </summary>
        /// <value>
        /// The shape.
        /// </value>
        GraphvizVertexShape Shape { get; }
    }
}