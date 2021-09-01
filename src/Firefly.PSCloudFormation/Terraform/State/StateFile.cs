namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Firefly.PSCloudFormation.ChangeVisualisation;

    using Newtonsoft.Json;

    using QuikGraph;
    using QuikGraph.Graphviz;
    using QuikGraph.Graphviz.Dot;

    /// <summary>
    /// Deserialization of the Terraform state file
    /// </summary>
    internal class StateFile
    {
        /// <summary>
        /// The serial number
        /// </summary>
        private int serial;

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the terraform version.
        /// </summary>
        /// <value>
        /// The terraform version.
        /// </value>
        [JsonProperty("terraform_version")]
        public string TerraformVersion { get; set; }

        /// <summary>
        /// Gets or sets the serial number.
        /// </summary>
        /// <value>
        /// The serial number which, when retrieved is incremented so that the next write of the file has a new serial number.
        /// </value>
        [JsonProperty("serial")]
        public int Serial
        {
            // Increment serial when writing out.
            get => this.serial + 1;
            set => this.serial = value;
        }

        /// <summary>
        /// Gets or sets the lineage.
        /// </summary>
        /// <value>
        /// The lineage.
        /// </value>
        [JsonProperty("lineage")]
        public Guid Lineage { get; set; }

        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        [JsonProperty("resources")]
        public List<Resource> Resources { get; set; }

        /// <summary>
        /// Generates the change graph.
        /// </summary>
        /// <returns>A digraph showing relationships between resources</returns>
        public BidirectionalGraph<Resource, TaggedEdge<Resource, ResourceDependency>> GenerateChangeGraph()
        {
            var edges = new List<TaggedEdge<Resource, ResourceDependency>>();
            var graph = new BidirectionalGraph<Resource, TaggedEdge<Resource, ResourceDependency>>();

            foreach (var r1 in this.Resources)
            {
                edges.AddRange(
                    from r2 in this.Resources.Where(r => r.ResourceInstance.Id != r1.ResourceInstance.Id)
                    let deps = r2.ResourceInstance.References(r1)
                    where deps != null
                    select new TaggedEdge<Resource, ResourceDependency>(r1, r2, deps));
            }

            graph.AddVertexRange(this.Resources);
            graph.AddEdgeRange(edges);
            return graph;
        }

        /// <summary>
        /// Generates the dot graph.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <returns>A DOT format digraph, primarily for use in debugging.</returns>
        public string GenerateDotGraph(BidirectionalGraph<Resource, TaggedEdge<Resource, ResourceDependency>> graph)
        {
            var dotGraph = graph.ToGraphviz(
                algorithm =>
                    {
                        var font = new GraphvizFont("Arial", 9);

                        algorithm.CommonVertexFormat.Font = font;
                        algorithm.CommonEdgeFormat.Font = font;
                        algorithm.GraphFormat.RankDirection = GraphvizRankDirection.LR;
                        algorithm.FormatVertex += (sender, args) =>
                            {
                                args.VertexFormat.Label = args.Vertex.Name;
                                args.VertexFormat.Shape = GraphvizVertexShape.Box;
                            };

                        algorithm.FormatEdge += (sender, args) => { args.EdgeFormat.Label.Value = args.Edge.Tag?.TargetAttribute; };
                    });

            // Temp fix - wait for https://github.com/KeRNeLith/QuikGraph/issues/27
            return DotHtmlFormatter.QuoteHtml(dotGraph);
        }

        /// <summary>
        /// Saves the state file to the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public void Save(string path)
        {
            if (File.Exists(path))
            {
                var backup = $"{path}.backup";

                if (File.Exists(backup))
                {
                    File.Delete(backup);
                    File.Move(path, backup);
                }

                File.Delete(path);
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented), new UTF8Encoding(false));
        }
    }
}