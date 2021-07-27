namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Xml;
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
        /// Determines whether this instance [can render SVG].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance [can render SVG]; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanRenderSvg()
        {
            try
            {
                var request = WebRequest.Create("https://quickchart.io/graphviz?graph=graph{a--b}");
                request.Method = "GET";
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var streamResponse = response.GetResponseStream();

                    if (streamResponse is null)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Renders the change details as SVG directed graph.
        /// </summary>
        /// <returns>SVG fragment for adding to browser view.</returns>
        public string RenderSvg()
        {
            var graph = this.GenerateChangeGraph();

            var dotGraph = graph.ToGraphviz(
                algorithm =>
                    {
                        // Custom init example
                        algorithm.FormatVertex += (sender, args) =>
                            {
                                args.VertexFormat.Label = args.Vertex.ToString();
                                if (args.Vertex is ResourceVertex rv)
                                {
                                    args.VertexFormat.FillColor =
                                        rv.Change.ResourceChange.Replacement == Replacement.True
                                            ? GraphvizColor.Red
                                            :
                                            rv.Change.ResourceChange.Replacement == Replacement.Conditional
                                                ?
                                                GraphvizColor.Orange
                                                : GraphvizColor.LightGreen;

                                    args.VertexFormat.Style = GraphvizVertexStyle.Filled;
                                }
                            };
                        algorithm.FormatEdge += (sender, args) => { args.EdgeFormat.Label.Value = args.Edge.Tag; };
                    });

            var uri = new UriBuilder("https://quickchart.io/graphviz")
                          {
                              Query = $"graph={HttpUtility.UrlEncode(dotGraph)}"
                          };

            var request = WebRequest.Create(uri.ToString());
            request.Method = "GET";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var streamResponse = response.GetResponseStream();

                if (streamResponse is null)
                {
                    return string.Empty;
                }

                var doc = XDocument.Load(streamResponse); // Svg

                var elem = doc.Elements().First();

                var sb = new StringBuilder();
                var xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };

                using (var xw = XmlWriter.Create(sb, xws))
                {
                    elem.WriteTo(xw);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Generates the change graph.
        /// </summary>
        public BidirectionalGraph<IChangeVertex, TaggedEdge<IChangeVertex, string>> GenerateChangeGraph()
        {
            var parameters = new List<ParameterVertex>();
            var direct = new DirectModificationVertex();
            var resourceVertices = this.Changes.Select(c => new ResourceVertex(c)).ToList();
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
                        edges.Add(new TaggedEdge<IChangeVertex, string>(direct, resource, detail.Target.Name));
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
                            r => r.Name == detail.CausingEntity.Split(new[] { '.' }).First());
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