namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System;
    using System.Diagnostics;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Base class for vertices displaying CloudFormation resources
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.ChangeVisualisation.IChangeVertex" />
    [DebuggerDisplay("Resource: {Name}")]
    internal abstract class ResourceVertex : IChangeVertex
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceVertex"/> class.
        /// </summary>
        /// <param name="change">The change.</param>
        public ResourceVertex(Change change)
        {
            this.Change = change;
        }

        /// <summary>
        /// Gets the change.
        /// </summary>
        /// <value>
        /// The change.
        /// </value>
        public Change Change { get; }

        /// <summary>
        /// Gets the color of the fill.
        /// </summary>
        /// <value>
        /// The color of the fill.
        /// </value>
        public abstract GraphvizColor FillColor { get; }

        /// <summary>
        /// Gets the color of the font.
        /// </summary>
        /// <value>
        /// The color of the font.
        /// </value>
        public abstract GraphvizColor FontColor { get; }

        /// <summary>
        /// Gets the label.
        /// </summary>
        /// <value>
        /// The label.
        /// </value>
        public abstract string Label { get; }

        /// <summary>
        /// Gets the name of the object represented by the vertex.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => this.Change.ResourceChange.LogicalResourceId;

        /// <summary>
        /// Gets the shape with which to draw the vertex.
        /// </summary>
        /// <value>
        /// The shape.
        /// </value>
        public GraphvizVertexShape Shape => GraphvizVertexShape.Box;

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>
        /// The style.
        /// </value>
        public abstract GraphvizVertexStyle Style { get; }

        /// <summary>
        /// Factory method to create resource vertex subclasses.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <returns>Appropriate subclass</returns>
        /// <exception cref="ArgumentException">Unsupported change action: {change.ResourceChange.Action}</exception>
        public static ResourceVertex Create(Change change)
        {
            if (change.ResourceChange.Action == ChangeAction.Add)
            {
                return new NewResourceVertex(change);
            }

            if (change.ResourceChange.Action == ChangeAction.Modify)
            {
                return new ModifiedResourceVertex(change);
            }

            if (change.ResourceChange.Action == ChangeAction.Remove)
            {
                return new DeletedResouceVertex(change);
            }

            if (change.ResourceChange.Action == ChangeAction.Import)
            {
                return new ImportedResourceVertex(change);
            }

            throw new ArgumentException($"Unsupported change action: {change.ResourceChange.Action}");
        }

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