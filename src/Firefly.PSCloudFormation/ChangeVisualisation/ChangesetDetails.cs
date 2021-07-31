﻿namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using QuikGraph;
    using QuikGraph.Graphviz;
    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Entity for formatting HTML change detail
    /// </summary>
    internal class ChangesetDetails
    {
        /// <summary>
        /// Gets or sets the Stack name
        /// </summary>
        // ReSharper disable UnusedAutoPropertyAccessor.Local - Used implicitly by json conversion
        public string StackName { get; set; }

        /// <summary>
        /// Gets or sets the Changeset Name
        /// </summary>
        public string ChangeSetName { get; set; }

        /// <summary>
        /// Gets or sets the Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the Changeset creation time
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the Detailed resource changes
        /// </summary>
        public List<Change> Changes { get; set; }

        // ReSharper restore UnusedAutoPropertyAccessor.Local

        /// <summary>
        /// Renders the change details as SVG directed graph.
        /// </summary>
        /// <param name="renderer">SVG renderer to use.</param>
        /// <returns><see cref="XElement"/> containing SVG fragment for adding to browser view.</returns>
        public async Task<XElement> RenderSvg(ISvgRenderer renderer)
        {
            var graph = this.GenerateChangeGraph();

            var dotGraph = graph.ToGraphviz(
                algorithm =>
                    {
                        var font = new GraphvizFont("Arial", 9);

                        algorithm.CommonVertexFormat.Font = font;
                        algorithm.CommonEdgeFormat.Font = font;
                        algorithm.GraphFormat.RankDirection = GraphvizRankDirection.LR;
                        algorithm.FormatVertex += (sender, args) =>
                            {
                                args.VertexFormat.Label = args.Vertex.ToString();
                                args.VertexFormat.Shape = args.Vertex.Shape;

                                if (args.Vertex is ResourceVertex rv)
                                {
                                    args.VertexFormat.Label = rv.Label;
                                    args.VertexFormat.Style = rv.Style;
                                    args.VertexFormat.FillColor = rv.FillColor;
                                    args.VertexFormat.FontColor = rv.FontColor;
                                }
                            };

                        algorithm.FormatEdge += (sender, args) => { args.EdgeFormat.Label.Value = args.Edge.Tag; };
                    });

            // Temp fix - wait for https://github.com/KeRNeLith/QuikGraph/issues/27
            dotGraph = DotHtmlFormatter.QuoteHtml(dotGraph);

            var svg = await renderer.RenderSvg(dotGraph);
            
            // Now change the width and height attributes of the SVG to 100% so it becomes responsive in bootstrap
            if (svg.Attribute("width") != null)
            {
                svg.Attribute("width").Value = "100%";
            }

            if (svg.Attribute("height") != null)
            {
                svg.Attribute("height").Value = "100%";
            }
            
            return svg;
        }

        /// <summary>
        /// Generates the change graph.
        /// </summary>
        /// <returns>A graph representation of the interaction between resources during the update.</returns>
        public BidirectionalGraph<IChangeVertex, TaggedEdge<IChangeVertex, string>> GenerateChangeGraph()
        {
            var parameters = new List<ParameterVertex>();
            var direct = new DirectModificationVertex();
            var resourceVertices = this.Changes.Select(ResourceVertex.Create).ToList();
            var edges = new List<TaggedEdge<IChangeVertex, string>>();
            var graph = new BidirectionalGraph<IChangeVertex, TaggedEdge<IChangeVertex, string>>();

            graph.Clear();

            // Create edge list
            foreach (var resource in resourceVertices)
            {
                foreach (var detail in resource.Change.ResourceChange.Details)
                {
                    if (detail.ChangeSource == ChangeSource.ParameterReference)
                    {
                        // Value of stack parameter has changed
                        var param = parameters.FirstOrDefault(p => p.Name == detail.CausingEntity);

                        if (param == null)
                        {
                            param = new ParameterVertex(detail.CausingEntity);
                            parameters.Add(param);
                        }

                        edges.Add(new TaggedEdge<IChangeVertex, string>(param, resource, detail.Target.Name));
                    }

                    if (detail.ChangeSource == ChangeSource.DirectModification
                        && detail.Evaluation == EvaluationType.Static)
                    {
                        // User directly modified a property
                        var edgeTag = detail.Target.Name ?? (detail.Target.Attribute == "Tags" ? "Tags" : null);

                        edges.Add(new TaggedEdge<IChangeVertex, string>(direct, resource, edgeTag));
                    }

                    if (detail.ChangeSource == ChangeSource.ResourceReference)
                    {
                        // Change via Ref to another resource
                        var causingEntity = resourceVertices.First(r => r.Name == detail.CausingEntity);
                        edges.Add(new TaggedEdge<IChangeVertex, string>(causingEntity, resource, detail.Target.Name));
                    }

                    if (detail.ChangeSource == ChangeSource.ResourceAttribute)
                    {
                        // Change via GetAtt from another resource
                        var causingEntity = resourceVertices.First(
                            r => r.Name == detail.CausingEntity.Split('.').First());
                        edges.Add(new TaggedEdge<IChangeVertex, string>(causingEntity, resource, detail.Target.Name));
                    }
                }
            }

            if (edges.Any(e => e.Source.GetType() == typeof(DirectModificationVertex)))
            {
                graph.AddVertex(direct);
            }

            graph.AddVertexRange(parameters);
            graph.AddVertexRange(resourceVertices);
            graph.AddEdgeRange(edges);

            return graph;
        }
    }
}