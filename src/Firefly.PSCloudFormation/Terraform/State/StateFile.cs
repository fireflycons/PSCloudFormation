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

    internal class StateFile
    {
        private int serial;

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("terraform_version")]
        public Version TerraformVersion { get; set; }

        [JsonProperty("serial")]
        public int Serial
        {
            // Increment serial when writing out.
            get => this.serial + 1;
            set => this.serial = value;
        }
        
        [JsonProperty("lineage")]
        public Guid Lineage { get; set; }

        [JsonProperty("resources")]
        public List<Resource> Resources { get; set; }

        public BidirectionalGraph<Resource, TaggedEdge<Resource, ResourceDependency>> GenerateChangeGraph()
        {
            var edges = new List<TaggedEdge<Resource, ResourceDependency>>();
            var graph = new BidirectionalGraph<Resource, TaggedEdge<Resource, ResourceDependency>>();

            foreach (var r1 in this.Resources)
            {
                foreach (var r2 in this.Resources.Where(r => r.ResourceInstance.Id != r1.ResourceInstance.Id))
                {
                    var deps = r2.ResourceInstance.References(r1);

                    if (deps != null)
                    {
                        edges.Add(new TaggedEdge<Resource, ResourceDependency>(r1, r2, deps));
                    }
                }
            }

            graph.AddVertexRange(this.Resources);
            graph.AddEdgeRange(edges);
            return graph;
        }

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