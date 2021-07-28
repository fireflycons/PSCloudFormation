namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Diagnostics;

    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Vertex class representing parameter block
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.ChangeVisualisation.IChangeVertex" />
    [DebuggerDisplay("Parameter: {Name}")]
    internal class ParameterVertex : IChangeVertex
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterVertex"/> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        public ParameterVertex(string parameterName)
        {
            this.Name = parameterName;
        }

        /// <summary>
        /// Gets the name of the object represented by the vertex.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the shape with which to draw the vertex.
        /// </summary>
        /// <value>
        /// The shape.
        /// </value>
        public GraphvizVertexShape Shape => GraphvizVertexShape.Diamond;

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}